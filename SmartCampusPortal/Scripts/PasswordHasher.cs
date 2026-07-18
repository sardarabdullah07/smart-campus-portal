using System;
using System.Security.Cryptography;
using System.Text;

namespace SmartCampusPortal
{
    /// <summary>
    /// Salted PBKDF2 password hashing for the Users.Password column.
    ///
    /// Stored format (single NVARCHAR column):
    ///     v1:&lt;iterations&gt;:&lt;base64 salt&gt;:&lt;base64 subkey&gt;
    /// e.g. v1:100000:Zm9vYmFyYmF6cXV4MTIz:...
    ///
    /// Anything that does NOT start with "v1:" is treated as a legacy plaintext
    /// password so that existing databases keep working; callers are told via the
    /// needsUpgrade out-parameter so they can rewrite the row with a real hash.
    ///
    /// Targets .NET Framework 4.7.2: uses the Rfc2898DeriveBytes overload that
    /// accepts a HashAlgorithmName, and hand-rolls the constant-time comparison
    /// because CryptographicOperations.FixedTimeEquals does not exist here.
    /// </summary>
    public static class PasswordHasher
    {
        private const string Prefix = "v1";
        private const int DefaultIterations = 100000;
        private const int SaltByteLength = 16;
        private const int SubkeyByteLength = 32;

        /// <summary>
        /// Hashes a password with a fresh random salt and returns the versioned
        /// string to store in the database.
        /// </summary>
        public static string Hash(string password)
        {
            if (password == null)
            {
                throw new ArgumentNullException("password");
            }

            byte[] salt = new byte[SaltByteLength];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }

            byte[] subkey = DeriveKey(password, salt, DefaultIterations, SubkeyByteLength);

            return string.Join(":",
                Prefix,
                DefaultIterations.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Convert.ToBase64String(salt),
                Convert.ToBase64String(subkey));
        }

        /// <summary>
        /// Verifies a password against a stored value.
        /// </summary>
        /// <param name="password">The plaintext password supplied by the user.</param>
        /// <param name="stored">The value currently in the Password column.</param>
        /// <param name="needsUpgrade">
        /// Set to true when the stored value matched but was a legacy plaintext
        /// entry. The caller should re-hash the password and UPDATE the row.
        /// </param>
        public static bool Verify(string password, string stored, out bool needsUpgrade)
        {
            needsUpgrade = false;

            if (password == null || string.IsNullOrEmpty(stored))
            {
                return false;
            }

            // Legacy plaintext row: compare directly, and flag for upgrade on match.
            if (!stored.StartsWith(Prefix + ":", StringComparison.Ordinal))
            {
                bool legacyMatch = FixedTimeEquals(
                    Encoding.UTF8.GetBytes(password),
                    Encoding.UTF8.GetBytes(stored));
                needsUpgrade = legacyMatch;
                return legacyMatch;
            }

            string[] parts = stored.Split(':');
            if (parts.Length != 4)
            {
                return false;
            }

            int iterations;
            if (!int.TryParse(parts[1], System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out iterations)
                || iterations <= 0)
            {
                return false;
            }

            byte[] salt;
            byte[] expectedSubkey;
            try
            {
                salt = Convert.FromBase64String(parts[2]);
                expectedSubkey = Convert.FromBase64String(parts[3]);
            }
            catch (FormatException)
            {
                // Corrupt/garbage row - treat as a failed login rather than throwing.
                return false;
            }

            if (salt.Length == 0 || expectedSubkey.Length == 0)
            {
                return false;
            }

            byte[] actualSubkey = DeriveKey(password, salt, iterations, expectedSubkey.Length);

            return FixedTimeEquals(actualSubkey, expectedSubkey);
        }

        private static byte[] DeriveKey(string password, byte[] salt, int iterations, int outputLength)
        {
            using (Rfc2898DeriveBytes pbkdf2 =
                new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(outputLength);
            }
        }

        /// <summary>
        /// Constant-time byte comparison. Walks every byte of both buffers and
        /// accumulates differences with XOR/OR instead of returning early, so the
        /// running time does not depend on where the first mismatch occurs.
        /// Length inequality is folded into the accumulator the same way.
        /// </summary>
        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null)
            {
                return false;
            }

            int difference = a.Length ^ b.Length;

            // Iterate over a fixed span (the longer of the two) so the loop count
            // does not leak the length of either buffer via an early exit.
            int length = a.Length > b.Length ? a.Length : b.Length;
            for (int i = 0; i < length; i++)
            {
                byte x = i < a.Length ? a[i] : (byte)0;
                byte y = i < b.Length ? b[i] : (byte)0;
                difference |= x ^ y;
            }

            return difference == 0;
        }
    }
}

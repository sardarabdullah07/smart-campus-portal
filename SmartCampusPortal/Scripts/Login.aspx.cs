using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Security;
using System.Web.UI;

namespace SmartCampusPortal
{
    public partial class Login : Page
    {
        /// <summary>
        /// A real PBKDF2 hash of a random throwaway secret, used only when the
        /// submitted email matches no row. Verifying against it burns the same
        /// 100,000 iterations a genuine account would, so response time does not
        /// reveal whether an email exists. It cannot match any user's password.
        /// </summary>
        private const string DummyHash =
            "v1:100000:hnypK5zJ8CeCB/svjT2K3Q==:UDxslIN8UrDkZwakL6XMbTZX5ZSiIcGqZ0cjUErDdD8=";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Request.IsAuthenticated)
                {
                    string role = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).UserData;
                    RedirectUser(role);
                }
            }
        }

        protected void btnLogin_Click(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                litMessage.Text = "<div class='alert alert-warning mt-3'>Please enter both email and password.</div>";
                return;
            }

            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["SmartCampusPortalConnection"].ConnectionString;

                string role = null;
                string userId = null;
                string fullName = null;
                string storedPassword = null;

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    // The password can no longer be matched in SQL: every row carries its
                    // own salt, so the hash is verified in C# after the row is fetched.
                    string query = "SELECT UserID, FullName, Role, Password FROM Users WHERE Email = @Email";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);
                        con.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                role = reader["Role"].ToString();
                                userId = reader["UserID"].ToString();
                                fullName = reader["FullName"].ToString();
                                storedPassword = reader["Password"].ToString();
                            }
                        }
                    }

                    bool needsUpgrade = false;
                    bool passwordOk;

                    if (storedPassword == null)
                    {
                        // Unknown email. Still run a full PBKDF2 derivation against a dummy
                        // hash so that a nonexistent account costs the same wall-clock time
                        // as a wrong password, and does not leak which emails are registered.
                        PasswordHasher.Verify(password, DummyHash, out needsUpgrade);
                        needsUpgrade = false;
                        passwordOk = false;
                    }
                    else
                    {
                        passwordOk = PasswordHasher.Verify(password, storedPassword, out needsUpgrade);
                    }

                    if (passwordOk)
                    {
                        // Transparent migration: the row still held a plaintext password,
                        // so replace it with a real salted hash now that we know it is valid.
                        if (needsUpgrade)
                        {
                            using (SqlCommand upgradeCmd = new SqlCommand(
                                "UPDATE Users SET Password = @Password WHERE UserID = @UserID", con))
                            {
                                upgradeCmd.Parameters.AddWithValue("@Password", PasswordHasher.Hash(password));
                                upgradeCmd.Parameters.AddWithValue("@UserID", Convert.ToInt32(userId));
                                upgradeCmd.ExecuteNonQuery();
                            }
                        }

                        FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(
                            1, // version
                            email, // user name
                            DateTime.Now, // creation
                            DateTime.Now.AddMinutes(30), // expiration
                            false, // persistent
                            role // user data (roles)
                        );

                        string encryptedTicket = FormsAuthentication.Encrypt(ticket);
                        Response.Cookies.Add(new System.Web.HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket));

                        Session["UserID"] = userId;
                        Session["FullName"] = fullName;
                        Session["UserRole"] = role;

                        RedirectUser(role);
                    }
                    else
                    {
                        // Identical message for "no such email" and "wrong password".
                        litMessage.Text = "<div class='alert alert-danger mt-3'>Invalid email or password.</div>";
                    }
                }
            }
            catch (SqlException ex)
            {
                litMessage.Text = "<div class='alert alert-danger mt-3'>Database error: " + ex.Message + "</div>";
            }
            catch (Exception ex)
            {
                litMessage.Text = "<div class='alert alert-danger mt-3'>An unexpected error occurred: " + ex.Message + "</div>";
            }
        }

        private void RedirectUser(string role)
        {
            switch (role)
            {
                case "Admin":
                    Response.Redirect("adminDashboard.aspx");
                    break;
                case "Student":
                    Response.Redirect("studentDashboard.aspx");
                    break;
                case "Faculty":
                    Response.Redirect("facultyDashboard.aspx");
                    break;
                default:
                    FormsAuthentication.SignOut();
                    Response.Redirect("Login.aspx");
                    break;
            }
        }
    }
}
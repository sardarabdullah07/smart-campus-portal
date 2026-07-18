# Smart Campus Portal

Smart Campus Portal is an ASP.NET Web Forms application for managing the day-to-day academic operations of a small campus. A single login page authenticates users against a `Users` table and routes them, by role, to one of three separate dashboards: administrators manage users and courses, faculty publish assignments, mark attendance, grade work and post announcements, and students register for courses, submit assignments, check grades and attendance-driven records, and pay outstanding fees. Data access is done with ADO.NET (`SqlConnection` / `SqlCommand` with parameterised queries) against SQL Server, with a LINQ to SQL data context (`Database.dbml`) generated over the same schema.

A live demo has previously been hosted at http://SmartCampusPortal.somee.com.

## Features

Authentication is handled by `Scripts/Login.aspx`. On a successful login the user's `UserID`, `FullName` and `Role` are stored in session, a Forms Authentication ticket is issued carrying the role in its `UserData`, and the user is redirected to the dashboard for their role. Every page re-checks `Session["UserRole"]` on load and signs out anyone whose role does not match.

### Admin

- **Dashboard** (`adminDashboard.aspx`) — headline counts of students, faculty and courses, plus a box for posting campus-wide announcements (stored with a `NULL` `CourseID` so they are visible to everyone) and a list of the ten most recent global announcements.
- **Manage Users** (`adminManageUsers.aspx`) — search users by name, email or role; add a user (creating the matching `Students` or `Faculty` row automatically); edit name/email/role inline in a GridView; delete a user, with dependent fee records, submissions, registrations, attendance and announcements removed inside a transaction.
- **Manage Courses** (`adminManageCourses.aspx`) — search courses by name or department; add a course with department, credit count and an optional prerequisite chosen from existing courses; edit courses inline; delete a course and cascade-remove its announcements, attendance, submissions, assignments and registrations in a transaction.
- **Attendance Report** (`adminViewAttendanceReport.aspx`) — cross-campus attendance view filterable by course, student, date, status, and a free-text name search over students and teachers.

### Faculty

- **Dashboard** (`facultyDashboard.aspx`) — number of courses taught, count of submissions still awaiting a grade, and the five most recent announcements relevant to the lecturer.
- **Upload Assignment** (`facultyUploadAssignment.aspx`) — create an assignment with title, description and due date, optionally attaching a specification document that is stored in the database as binary content; edit and delete existing assignments (deleting an assignment removes its submissions transactionally); download any previously attached document.
- **Grade Students** (`facultyGradeStudents.aspx`) — load student submissions per course, download each submitted file, and set a 0–100 grade inline; separately record standalone grades by category (Quiz, Paper, Project, Presentation) with a score, max score and remarks.
- **View / Mark Attendance** (`facultyViewAttendance.aspx`) — filter existing attendance by course, date and status, and mark attendance for a chosen course and date, where each enrolled student gets a Present/Absent dropdown; saving inserts new rows or updates existing ones for that date.
- **Send Announcement** (`facultySendAnnouncement.aspx`) — post an announcement either to a specific course or as a general announcement, review previously posted announcements, and delete them.

### Student

- **Dashboard** (`studentDashboard.aspx`) — number of registered courses, total outstanding fees, and the five most recent announcements that are either general or attached to a registered course.
- **Course Registration** (`studentCourseRegistration.aspx`) — browse courses not yet registered for (with prerequisite shown), register with automatic prerequisite enforcement, and drop a registered course.
- **Assignment Upload** (`studentAssignmentUpload.aspx`) — see every assignment for registered courses with its status (Not submitted / Submitted / Graded), download the assignment specification, and upload a submission. Uploads are validated for extension (`.pdf`, `.doc`, `.docx`, `.txt`, `.zip`, `.rar`) and a 5 MB size limit, and re-uploading replaces the previous submission rather than duplicating it.
- **View Grades** (`studentViewGrades.aspx`) — pick a registered course and see assignment grades alongside separate tables for Quiz, Paper, Project and Presentation grades.
- **Fee Record** (`studentFeeRecord.aspx`) — total billed, total paid and outstanding balance, a breakdown of individual fee records, and a card payment form (with cardholder, card number, MM/YY expiry and CVV validation) that applies the payment across outstanding records oldest-first and writes a payment reference.
- **Profile** (`studentViewProfile.aspx`) — personal details from `Users`/`Students` plus an academic record of registered courses.

## Tech Stack

| Layer | Technology |
| --- | --- |
| Runtime | .NET Framework 4.7.2 (`TargetFrameworkVersion` `v4.7.2`) |
| Web framework | ASP.NET Web Forms (Master Pages, GridView, FileUpload, Forms Authentication) |
| Routing | Microsoft ASP.NET FriendlyUrls 1.0.2 |
| Data access | ADO.NET (`System.Data.SqlClient`) and LINQ to SQL (`Database.dbml` → `DatabaseDataContext`) |
| Database | SQL Server (developed against SQL Server Express) |
| Front end | Bootstrap 5.2.3, jQuery 3.7.0, Modernizr 2.8.3 |
| Bundling | Microsoft.AspNet.Web.Optimization 1.1.3 (+ WebForms integration), WebGrease 1.6.0 |
| Compiler | Microsoft.CodeDom.Providers.DotNetCompilerPlatform 2.0.1 (Roslyn) |
| Language | C# |

## Project Structure

```
Practice-ASP.NET-Web-Forms-Project-main/
├── SmartCampusPortal.sln              Visual Studio solution (single web project)
└── SmartCampusPortal/
    ├── App_Start/
    │   ├── BundleConfig.cs            Script bundles and the jQuery ScriptManager mapping
    │   └── RouteConfig.cs             Enables FriendlyUrls (extensionless URLs)
    ├── Content/
    │   ├── bootstrap*.css             Bootstrap 5.2.3 stylesheets
    │   ├── Site.css                   Default template styles
    │   ├── portal.css                 Portal-specific styling
    │   └── portal.js                  Portal-specific client script
    ├── Properties/AssemblyInfo.cs     Assembly metadata
    ├── Scripts/                       See note below — both the app's pages and vendor JS
    │   ├── Login.aspx(.cs)            Login form and role-based redirect
    │   ├── adminDashboard.aspx(.cs)   Admin landing page and global announcements
    │   ├── adminManageUsers.aspx      User search, create, edit, cascade delete
    │   ├── adminManageCourses.aspx    Course search, create, edit with prerequisites, cascade delete
    │   ├── adminViewAttendanceReport.aspx  Campus-wide attendance report with filters
    │   ├── facultyDashboard.aspx      Faculty stats and recent announcements
    │   ├── facultyUploadAssignment.aspx    Assignment CRUD with document attachments
    │   ├── facultyGradeStudents.aspx  Submission grading plus category grades
    │   ├── facultyViewAttendance.aspx View and mark attendance per course/date
    │   ├── facultySendAnnouncement.aspx    Course and general announcements
    │   ├── studentDashboard.aspx      Student stats and announcements
    │   ├── studentCourseRegistration.aspx  Register/drop courses with prerequisite checks
    │   ├── studentAssignmentUpload.aspx    Assignment list, downloads and submission upload
    │   ├── studentViewGrades.aspx     Grades by course and category
    │   ├── studentFeeRecord.aspx      Fee summary, breakdown and card payment form
    │   ├── studentViewProfile.aspx    Profile and academic record
    │   ├── Database.dbml(.designer.cs)     LINQ to SQL data context over SmartCampusDB
    │   ├── SQLQueryDatabase.sql       Schema creation script with sample data
    │   ├── jquery-3.7.0*.js           jQuery 3.7.0 distribution
    │   ├── bootstrap*.js              Bootstrap 5.2.3 JavaScript bundles
    │   ├── modernizr-2.8.3.js         Feature detection
    │   └── WebForms/                  ASP.NET Web Forms client script library
    ├── Site.Master(.cs)               Shared desktop layout, navbar and ScriptManager
    ├── Site.Mobile.Master(.cs)        Mobile layout variant
    ├── ViewSwitcher.ascx(.cs)         Desktop/mobile view switch control
    ├── Default.aspx / About.aspx / Contact.aspx   Default template pages
    ├── Global.asax(.cs)               Application startup: route and bundle registration
    ├── Web.config                     Connection strings, framework and compiler settings
    ├── Web.Debug.config / Web.Release.config     Config transforms
    ├── Bundle.config                  CSS bundle definition
    └── packages.config                NuGet package list
```

> **Note on the `Scripts/` folder.** Unlike the usual Web Forms convention, every application `.aspx` page lives inside `SmartCampusPortal/Scripts/` rather than at the project root or in role-named subfolders. That folder therefore mixes application pages with the vendor JavaScript that normally belongs there (jQuery, Bootstrap, Modernizr, and the `WebForms/` script library), as well as the `Database.dbml` data context and the SQL schema script. This is why redirects in the code are written as `~/Scripts/Login.aspx`. Only the default template pages (`Default.aspx`, `About.aspx`, `Contact.aspx`) sit at the project root.

## Prerequisites

- **Visual Studio 2022** (the solution was last saved with Visual Studio 17.x) with the **ASP.NET and web development** workload installed.
- **.NET Framework 4.7.2 Developer Pack**.
- **SQL Server Express** (or any SQL Server edition) plus a management tool such as SQL Server Management Studio or the Visual Studio SQL Server Object Explorer.
- IIS Express, which ships with the Visual Studio web workload.

## Database Setup

1. Create the database. `Scripts/SQLQueryDatabase.sql` begins with `USE SmartCampusDB`, so the database itself must exist first. In SQL Server Management Studio (or `sqlcmd`), run:

   ```sql
   CREATE DATABASE SmartCampusDB;
   ```

2. Run the schema script. Open `SmartCampusPortal/Scripts/SQLQueryDatabase.sql` against the `SmartCampusDB` database and execute it. It drops and recreates the core tables — `Users`, `Students`, `Faculty`, `Courses`, `CourseRegistrations`, `Assignments`, `Submissions`, `FeeRecords`, `Attendance` and `Announcements` — and inserts sample data, including one user per role:

   | Email | Password | Role |
   | --- | --- | --- |
   | `admin1@campus.edu` | `pass123` | Admin |
   | `faculty1@campus.edu` | `pass123` | Faculty |
   | `student1@campus.edu` | `pass123` | Student |

   Because the script drops tables first, re-running it discards all existing data.

3. Create the additional objects the pages expect. Some features query tables and columns that are **not** created by `SQLQueryDatabase.sql` and must be added manually before those pages will work:

   - `CourseEnrollments` (student-to-course-to-faculty mapping) — used by faculty grading, faculty attendance and the admin attendance report.
   - `Grades` (`StudentID`, `CourseID`, `Category`, `Title`, `Score`, `MaxScore`, `Remarks`, `GradedBy`, `DateGraded`) — used by faculty category grading and the student grades page.
   - `Payments` (`StudentID`, `Amount`, `Method`, `Reference`) — used by the student fee payment form.
   - `FileName`, `FileContent` and `ContentType` columns on `Assignments` — used when a faculty member attaches a specification document.

4. Point the application at your server. `Web.config` ships with two connection strings, `SmartCampusPortalConnection` (used by all page code) and `SmartCampusDBConnectionString` (used by the LINQ to SQL context). **Both are checked in pointing at a specific developer machine (`DESKTOP-TN5CPPS\SQLEXPRESS`) and will not work as-is.** Edit the `Data Source` in both entries to match your own instance, for example:

   ```xml
   <connectionStrings>
     <add name="SmartCampusPortalConnection"
          connectionString="Data Source=.\SQLEXPRESS;Initial Catalog=SmartCampusDB;Integrated Security=True;Encrypt=False;TrustServerCertificate=True"
          providerName="System.Data.SqlClient" />
     <add name="SmartCampusDBConnectionString"
          connectionString="Data Source=.\SQLEXPRESS;Initial Catalog=SmartCampusDB;Integrated Security=True;Encrypt=False;TrustServerCertificate=True"
          providerName="System.Data.SqlClient" />
   </connectionStrings>
   ```

   Use `Data Source=(localdb)\MSSQLLocalDB` for LocalDB, or replace `Integrated Security=True` with `User ID=...;Password=...` for SQL authentication.

## Build and Run

### Visual Studio (IIS Express)

1. Open `SmartCampusPortal.sln` in Visual Studio.
2. Restore NuGet packages — right-click the solution and choose **Restore NuGet Packages**, or simply build, since restore runs automatically. The packages listed in `packages.config` are expected in a `packages/` folder next to the solution.
3. Build the solution (**Build → Build Solution**, or <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>B</kbd>).
4. Make sure the database exists and the connection strings have been updated as described above.
5. Press <kbd>F5</kbd> (or <kbd>Ctrl</kbd>+<kbd>F5</kbd> to run without debugging). The project is configured for IIS Express (`UseIISExpress` is `true`, SSL port `44370`).
6. Browse to `/Scripts/Login.aspx` and sign in with one of the sample accounts. FriendlyUrls is enabled, so `/Scripts/Login` also works. Each role is redirected to its own dashboard on success.

### Command line

With MSBuild from a Developer Command Prompt:

```
nuget restore SmartCampusPortal.sln
msbuild SmartCampusPortal.sln /p:Configuration=Debug
```

Then run the site from Visual Studio or deploy the built output to IIS.

### Deploying to IIS

Publish or copy the project output to a folder served by IIS, create an application pool targeting **.NET CLR v4.0** in **Integrated** pipeline mode, and grant the pool identity access to `SmartCampusDB` (or use a SQL login in the connection string instead of integrated security).

## Security Notes

This is a practice project and is not hardened for production use. In particular, passwords are stored and compared as plain text in the `Users` table, the Forms Authentication cookie is written manually rather than through `FormsAuthentication.SetAuthCookie`, authorisation is enforced per page via a session check rather than through `<authorization>` rules in `Web.config`, and raw exception messages are rendered to the page. Address these before using the application with real data.

namespace BugTracker.Web.Core.Identification
{
    internal static class ApplicationRole
    {
        public const string Administrator = "Administrator";
        public const string ProjectAdministrator = "ProjectAdministrator";
        public const string Member = "Member";
        public const string Guest = "Guest";

        public const string Administrators = Administrator + "," + ProjectAdministrator;
    }
}
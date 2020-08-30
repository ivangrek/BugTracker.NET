namespace BugTracker.Web.Core.Identification
{
    public static class BtNetRole
    {
        public const string Administrator = "Administrator";
        public const string ProjectAdministrator = "ProjectAdministrator";
        public const string User = "User";
        public const string Guest = "Guest";

        public const string Administrators = Administrator + "," + ProjectAdministrator;
    }
}

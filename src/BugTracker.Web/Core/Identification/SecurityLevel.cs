namespace BugTracker.Web.Core.Identification
{
    public enum SecurityLevel
    {
        MustBeAdmin = 1,
        AnyUserOk = 2,
        AnyUserOkExceptGuest = 3,
        MustBeAdminOrProjectAdmin = 4
    }
}
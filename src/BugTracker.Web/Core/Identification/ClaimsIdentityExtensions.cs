namespace BugTracker.Web.Core.Identification
{
    using System;
    using System.Security;
    using System.Security.Claims;
    using System.Security.Principal;

    public static class ClaimsIdentityExtensions
    {
        public static int GetUserId(this IIdentity identity)
        {
            return Convert.ToInt32(GetClaimsValue(identity, BtNetClaimType.UserId));
        }

        public static string GetEmail(this IIdentity identity)
        {
            return GetClaimsValue(identity, ClaimTypes.Email);
        }

        public static int GetOrganizationId(this IIdentity identity)
        {
            return Convert.ToInt32(GetClaimsValue(identity, BtNetClaimType.OrganizationId));
        }

        public static int GetForcedProjectId(this IIdentity identity)
        {
            return Convert.ToInt32(GetClaimsValue(identity, BtNetClaimType.ForcedProjectId));
        }

        public static int GetBugsPerPage(this IIdentity identity)
        {
            return Convert.ToInt32(GetClaimsValue(identity, BtNetClaimType.BugsPerPage));
        }

        public static bool GetEnablePopups(this IIdentity identity)
        {
            return Convert.ToBoolean(GetClaimsValue(identity, BtNetClaimType.EnablePopUps));
        }

        public static bool GetUseFCKEditor(this IIdentity identity)
        {
            return Convert.ToBoolean(GetClaimsValue(identity, BtNetClaimType.UseFCKEditor));
        }

        public static bool GetCanAddBugs(this IIdentity identity)
        {
            return Convert.ToBoolean(GetClaimsValue(identity, BtNetClaimType.CanAddBugs));
        }

        public static int GetOtherOrgsPermissionLevels(this IIdentity identity)
        {
            return Convert.ToInt32(GetClaimsValue(identity, BtNetClaimType.OtherOrgsPermissionLevel));
        }

        public static bool GetCanSearch(this IIdentity identity)
        {
            return Convert.ToBoolean(GetClaimsValue(identity, BtNetClaimType.CanSearch));
        }

        public static bool GetIsExternalUser(this IIdentity identity)
        {
            return Convert.ToBoolean(GetClaimsValue(identity, BtNetClaimType.IsExternalUser));
        }

        public static bool GetCanOnlySeeOwnReportedBugs(this IIdentity identity)
        {
            return Convert.ToBoolean(GetClaimsValue(identity, BtNetClaimType.CanOnlySeeOwnReportedBugs));
        }

        public static bool GetCanBeAssignedTo(this IIdentity identity)
        {
            return Convert.ToBoolean(GetClaimsValue(identity, BtNetClaimType.CanBeAssignedTo));
        }

        public static bool GetNonAdminsCanUse(this IIdentity identity)
        {
            return Convert.ToBoolean(GetClaimsValue(identity, BtNetClaimType.NonAdminsCanUse));
        }

        public static int GetProjectFieldPermissionLevel(this IIdentity identity)
        {
            return Convert.ToInt32(GetClaimsValue(identity, BtNetClaimType.ProjectFieldPermissionLevel));
        }

        public static int GetOrgFieldPermissionLevel(this IIdentity identity)
        {
            return Convert.ToInt32(GetClaimsValue(identity, BtNetClaimType.OrgFieldPermissionLevel));
        }

        public static int GetCategoryFieldPermissionLevel(this IIdentity identity)
        {
            return Convert.ToInt32(GetClaimsValue(identity, BtNetClaimType.CategoryFieldPermissionLevel));
        }

        public static int GetPriorityFieldPermissionLevel(this IIdentity identity)
        {
            return Convert.ToInt32(GetClaimsValue(identity, BtNetClaimType.PriorityFieldPermissionLevel));
        }

        public static int GetStatusFieldPermissionLevel(this IIdentity identity)
        {
            return Convert.ToInt32(GetClaimsValue(identity, BtNetClaimType.StatusFieldPermissionLevel));
        }

        public static int GetAssignedToFieldPermissionLevel(this IIdentity identity)
        {
            return Convert.ToInt32(GetClaimsValue(identity, BtNetClaimType.AssignedToFieldPermissionLevel));
        }

        public static int GetUdfFieldPermissionLevel(this IIdentity identity)
        {
            return Convert.ToInt32(GetClaimsValue(identity, BtNetClaimType.UdfFieldPermissionLevel));
        }

        public static int GetTagsFieldPermissionLevel(this IIdentity identity)
        {
            return Convert.ToInt32(GetClaimsValue(identity, BtNetClaimType.TagsFieldPermissionLevel));
        }

        public static bool GetCanEditSql(this IIdentity identity)
        {
            return Convert.ToBoolean(GetClaimsValue(identity, BtNetClaimType.CanEditSql));
        }

        public static bool GetCanDeleteBugs(this IIdentity identity)
        {
            return Convert.ToBoolean(GetClaimsValue(identity, BtNetClaimType.CanDeleteBugs));
        }

        public static bool GetCanEditAndDeletePosts(this IIdentity identity)
        {
            return Convert.ToBoolean(GetClaimsValue(identity, BtNetClaimType.CanEditAndDeletePosts));
        }

        public static bool GetCanMergeBugs(this IIdentity identity)
        {
            return Convert.ToBoolean(GetClaimsValue(identity, BtNetClaimType.CanMergeBugs));
        }

        public static bool GetCanMassEditBugs(this IIdentity identity)
        {
            return Convert.ToBoolean(GetClaimsValue(identity, BtNetClaimType.CanMassEditBugs));
        }

        public static bool GetCanUseReports(this IIdentity identity)
        {
            return Convert.ToBoolean(GetClaimsValue(identity, BtNetClaimType.CanUseReports));
        }

        public static bool GetCanEditReports(this IIdentity identity)
        {
            return Convert.ToBoolean(GetClaimsValue(identity, BtNetClaimType.CanEditReports));
        }

        public static bool GetCanViewTasks(this IIdentity identity)
        {
            return Convert.ToBoolean(GetClaimsValue(identity, BtNetClaimType.CanViewTasks));
        }

        public static bool GetCanEditTasks(this IIdentity identity)
        {
            return Convert.ToBoolean(GetClaimsValue(identity, BtNetClaimType.CanEditTasks));
        }

        public static bool GetCanAssignToInternalUsers(this IIdentity identity)
        {
            return Convert.ToBoolean(GetClaimsValue(identity, BtNetClaimType.CanAssignToInternalUsers));
        }

        //public static bool IsInRole(this IIdentity identity, string roleName)
        //{
        //    if (identity is ClaimsIdentity claimsIdentity)
        //    {
        //        return claimsIdentity.HasClaim(ClaimTypes.Role, roleName);
        //    }

        //    throw new SecurityException("Identity is not a valid Claims Identity");
        //}

        private static string GetClaimsValue(IIdentity identity, string claimType)
        {
            if (identity is ClaimsIdentity claimsIdentity)
            {
                var userIdClaim = claimsIdentity.FindFirst(claimType);

                if (userIdClaim != null)
                {
                    return userIdClaim.Value;
                }

                throw new SecurityException($"Identity is missing value for claim type {claimType}");
            }

            throw new SecurityException("Identity is not a valid Claims Identity");
        }
    }
}

/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core
{
    using System.Collections.Generic;
    using System.Web.Security;

    public static class ApplicationRoles
    {
        public const string Administrator = "Administrator";
        public const string ProjectAdministrator = "ProjectAdministrator";
        public const string Guest = "Guest";

        public const string Administrators = Administrator + "," + ProjectAdministrator;
    }

    public sealed class ApplicationRoleProvider : RoleProvider
    {
        public override string ApplicationName { get; set; }

        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            throw new System.NotImplementedException();
        }

        public override void CreateRole(string roleName)
        {
            throw new System.NotImplementedException();
        }

        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            throw new System.NotImplementedException();
        }

        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            throw new System.NotImplementedException();
        }

        public override string[] GetAllRoles()
        {
            var roles = new List<string>
            {
                ApplicationRoles.Administrator,
                ApplicationRoles.ProjectAdministrator,
                ApplicationRoles.Guest
            };

            return roles.ToArray();
        }

        public override string[] GetRolesForUser(string username)
        {
            var roles = new List<string>();

            // TODO investigate
            if (username == "guest")
            {
                roles.Add(ApplicationRoles.Guest);

                return roles.ToArray();
            }

            var sql = $@"
                SELECT
                    us_admin
                FROM
                    users
                WHERE
                    us_username = '{username}'";

            var dataRow = DbUtil.GetDataRow(sql);

            if (dataRow != null && (int)dataRow["us_admin"] == 1)
            {
                roles.Add(ApplicationRoles.Administrator);
            }

            // TODO investigate
            //roles.Add(ApplicationRoles.ProjectAdministrator");

            return roles.ToArray();
        }

        public override string[] GetUsersInRole(string roleName)
        {
            throw new System.NotImplementedException();
        }

        public override bool IsUserInRole(string username, string roleName)
        {
            throw new System.NotImplementedException();
        }

        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            throw new System.NotImplementedException();
        }

        public override bool RoleExists(string roleName)
        {
            throw new System.NotImplementedException();
        }
    }
}
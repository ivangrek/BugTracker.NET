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
        public const string Member = "Member";
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
                ApplicationRoles.Member,
                ApplicationRoles.Guest
            };

            return roles.ToArray();
        }

        public override string[] GetRolesForUser(string username)
        {
            var roles = new List<string>();

            if (username == "guest")
            {
                roles.Add(ApplicationRoles.Guest);

                return roles.ToArray();
            }

            var sql = $@"
                DECLARE @project_admin INT
                SELECT
                    @project_admin = COUNT(1)
                FROM
                    users

                    INNER JOIN
                        project_user_xref
                    ON
                        pu_id = us_id
                        AND
                        pu_admin = 1
                WHERE
                    us_username = '{username}'
                    AND
                    us_active = 1

                SELECT
                    us_admin,
                    @project_admin project_admin
                FROM
                    users
                WHERE
                    us_username = '{username}'
                    AND
                    us_active = 1";

            var dataRow = DbUtil.GetDataRow(sql);

            if (dataRow != null)
            {
                if ((int)dataRow["us_admin"] == 1)
                {
                    roles.Add(ApplicationRoles.Administrator);
                }
                else if ((int)dataRow["project_admin"] == 1)
                {
                    roles.Add(ApplicationRoles.ProjectAdministrator);
                }
                else
                {
                    roles.Add(ApplicationRoles.Member);
                }
            }

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
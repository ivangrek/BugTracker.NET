/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core.Identification
{
    using System;
    using System.Data;
    using System.Security.Authentication;
    using System.Web;

    public interface ISecurity
    {
        string AuthMethod { get; }

        User User { get; }
    }

    public sealed class Security : ISecurity
    {
        private readonly IApplicationSettings applicationSettings;

        public Security(IApplicationSettings applicationSettings)
        {
            this.applicationSettings = applicationSettings;
        }

        public string AuthMethod
        {
            get
            {
                if (this.applicationSettings.WindowsAuthentication == AuthenticationMode.Windows)
                {
                    return "windows";
                }

                return "plain";
            }
        }

        public User User
        {
            get
            {
                var identity = HttpContext.Current.User.Identity;

                if (!identity.IsAuthenticated)
                {
                    throw new AuthenticationException();
                }

                DataRow dr = null;

                if (identity.Name == "guest")
                {
                    var sql = @"
                        /* get guest  */
                        SELECT
                            us_id,
                            us_admin,
                            us_username,
                            us_firstname,
                            us_lastname,
                            isnull(us_email,'') us_email,
                            isnull(us_bugs_per_page,10) us_bugs_per_page,
                            isnull(us_forced_project,0) us_forced_project,
                            us_use_fckeditor,
                            us_enable_bug_list_popups,
                            og.*,
                            isnull(us_forced_project, 0 ) us_forced_project,
                            isnull(pu_permission_level, $dpl) pu_permission_level,
                            0 [project_admin]
                        FROM
                            users

                            INNER JOIN
                                orgs og
                            ON
                                us_org = og_id

                            LEFT OUTER JOIN
                                project_user_xref
                            ON
                                pu_project = us_forced_project
                                AND
                                pu_user = us_id
                        WHERE
                            us_username = 'guest'
                            AND
                            us_active = 1";

                    sql = sql.Replace("$dpl", this.applicationSettings.DefaultPermissionLevel.ToString());

                    dr = DbUtil.GetDataRow(sql);
                }
                else
                {
                    var sql = @"
                        /* check session */
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
                            us_username = '$username'
                            AND
                            us_active = 1;

                        SELECT
                            us_id,
                            us_admin,
                            us_username,
                            us_firstname,
                            us_lastname,
                            isnull(us_email,'') us_email,
                            isnull(us_bugs_per_page,10) us_bugs_per_page,
                            isnull(us_forced_project,0) us_forced_project,
                            us_use_fckeditor,
                            us_enable_bug_list_popups,
                            og.*,
                            isnull(us_forced_project, 0 ) us_forced_project,
                            isnull(pu_permission_level, $dpl) pu_permission_level,
                            @project_admin [project_admin]
                        FROM
                            users

                            INNER JOIN
                                orgs og
                            ON
                                us_org = og_id

                            LEFT OUTER JOIN
                                project_user_xref
                            ON
                                pu_project = us_forced_project
                                AND
                                pu_user = us_id
                        WHERE
                            us_username = '$username'
                            AND
                            us_active = 1";

                    sql = sql.Replace("$username", identity.Name);
                    sql = sql.Replace("$dpl", this.applicationSettings.DefaultPermissionLevel.ToString());

                    dr = DbUtil.GetDataRow(sql);
                }

                // no previous session, no guest login allowed
                if (dr == null)
                {
                    throw new InvalidOperationException("User must be.");
                }
                else
                {
                    var user = new User();

                    user.SetFromDb(dr);

                    return user;
                }
            }
        }
    }
}
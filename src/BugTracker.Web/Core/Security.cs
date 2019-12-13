/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core
{
    using System;
    using System.Data;
    using System.Security.Authentication;
    using System.Web;

    public enum SecurityLevel
    {
        MustBeAdmin = 1,
        AnyUserOk = 2,
        AnyUserOkExceptGuest = 3,
        MustBeAdminOrProjectAdmin = 4
    }

    public enum SecurityPermissionLevel
    {
        PermissionNone = 0,
        PermissionReadonly = 1,
        PermissionAll = 2,
        PermissionReporter = 3
    }

    public interface ISecurity
    {
        string AuthMethod { get; }

        User User { get; }

        void CheckSecurity(SecurityLevel level);

        void CreateSession(HttpRequest request, HttpResponse response, int userid, string username, string ntlm);
    }

    public sealed class Security : ISecurity
    {
        public static IApplicationSettings ApplicationSettings = new ApplicationSettings();

        public static readonly string GotoForm = @"
<td nowrap valign=middle>
    <form style='margin: 0px; padding: 0px;' action=" + VirtualPathUtility.ToAbsolute("~/Bugs/Edit.aspx?id=") + @" method=get>
        <input class=menubtn type=submit value='go to ID'>
        <input class=menuinput size=4 type=text class=txt name=id accesskey=g>
    </form>
</td>";

        public string AuthMethod
        {
            get
            {
                if (ApplicationSettings.WindowsAuthentication == 1)
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

                var aspNetContext = HttpContext.Current;

                //var request = aspNetContext.Request;
                //var cookie = request.Cookies["se_id2"];

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

                    sql = sql.Replace("$dpl", ApplicationSettings.DefaultPermissionLevel.ToString());

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
                    sql = sql.Replace("$dpl", ApplicationSettings.DefaultPermissionLevel.ToString());

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

        public void CheckSecurity(SecurityLevel level)
        {
            if (level == SecurityLevel.MustBeAdmin && !User.IsAdmin)
            {
                Util.WriteToLog("must be admin, redirecting");
                HttpContext.Current.Response.Redirect("~/Account/Login");
            }
            else if (level == SecurityLevel.AnyUserOkExceptGuest && User.IsGuest)
            {
                Util.WriteToLog("cant be guest, redirecting");
                HttpContext.Current.Response.Redirect("~/Account/Login");
            }
            else if (level == SecurityLevel.MustBeAdminOrProjectAdmin && !User.IsAdmin && !User.IsProjectAdmin)
            {
                Util.WriteToLog("must be project admin, redirecting");
                HttpContext.Current.Response.Redirect("~/Account/Login");
            }
        }

        public void CreateSession(HttpRequest request, HttpResponse response, int userid, string username, string ntlm)
        {
            // Generate a random session id
            // Don't use a regularly incrementing identity
            // column because that can be guessed.
            //var guid = Guid.NewGuid().ToString();

            //Util.WriteToLog("guid=" + guid);

            //var sql = @"insert into sessions (se_id, se_user) values('$gu', $us)";
            //sql = sql.Replace("$gu", guid);
            //sql = sql.Replace("$us", Convert.ToString(userid));

            //DbUtil.ExecuteNonQuery(sql);

            //HttpContext.Current.Session[guid] = userid;

            //var sAppPath = request.Url.AbsolutePath;
            //sAppPath = sAppPath.Substring(0, sAppPath.LastIndexOf('/'));
            //Util.WriteToLog("AppPath:" + sAppPath);

            //response.Cookies.Set(new HttpCookie("se_id2", guid));
            //response.Cookies["se_id2"].Path = sAppPath;
            response.Cookies["user"]["name"] = username;
            response.Cookies["user"]["NTLM"] = ntlm;
            //response.Cookies["user"].Path = sAppPath;
            var dt = DateTime.Now;
            var ts = new TimeSpan(365, 0, 0, 0);
            response.Cookies["user"].Expires = dt.Add(ts);
        }
    }
}
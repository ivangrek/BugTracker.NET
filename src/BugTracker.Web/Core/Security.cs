/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core
{
    using System;
    using System.Data;
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

        public string AuthMethod { get; private set; } = string.Empty;

        public User User { get; } = new User();

        public void CheckSecurity(SecurityLevel level)
        {
            var aspNetContext = HttpContext.Current;

            Util.SetContext(aspNetContext);
            var request = aspNetContext.Request;
            var response = aspNetContext.Response;
            var cookie = request.Cookies["se_id2"];

            // This logic allows somebody to put a link in an email, like
            // Bugs/Edit.aspx?id=66
            // The user would click on the link, go to the logon page (Account/Login),
            // and then after logging in continue on to Bugs/Edit.aspx?id=66
            var originalUrl = request.ServerVariables["URL"].ToLower();
            var originalQuerystring = request.ServerVariables["QUERY_STRING"].ToLower();

            var target = "~/Account/Login";

            if (originalUrl.EndsWith("MobileEdit.aspx")) target = "~/Accounts/MobileLogin.aspx";

            target += "?url=" + originalUrl + "&qs=" + HttpUtility.UrlEncode(originalQuerystring);

            DataRow dr = null;

            if (cookie == null)
            {
                if (!ApplicationSettings.AllowGuestWithoutLogin)
                {
                    Util.WriteToLog("se_id cookie is null, so redirecting");
                    response.Redirect(target);
                    return;
                }
            }
            else
            {
                // guard against "Sql Injection" exploit
                var seId = cookie.Value.Replace("'", "''");
                var userId = 0;
                var obj = aspNetContext.Session[seId];
                if (obj != null) userId = Convert.ToInt32(obj);

                // check for existing session for active user
                var sql = @"
/* check session */
declare @project_admin int
select @project_admin = count(1)
    from sessions
    inner join project_user_xref on pu_user = se_user
    and pu_admin = 1
    where se_id = '$se';

select us_id, us_admin,
us_username, us_firstname, us_lastname,
isnull(us_email,'') us_email,
isnull(us_bugs_per_page,10) us_bugs_per_page,
isnull(us_forced_project,0) us_forced_project,
us_use_fckeditor,
us_enable_bug_list_popups,
og.*,
isnull(us_forced_project, 0 ) us_forced_project,
isnull(pu_permission_level, $dpl) pu_permission_level,
@project_admin [project_admin]
from sessions
inner join users on se_user = us_id
inner join orgs og on us_org = og_id
left outer join project_user_xref
    on pu_project = us_forced_project
    and pu_user = us_id
where se_id = '$se'
and us_active = 1";

                sql = sql.Replace("$se", seId);
                sql = sql.Replace("$dpl", ApplicationSettings.DefaultPermissionLevel.ToString());
                dr = DbUtil.GetDataRow(sql);
            }

            if (dr == null)
                if (ApplicationSettings.AllowGuestWithoutLogin)
                {
                    // allow users in, even without logging on.
                    // The user will have the permissions of the "guest" user.
                    var sql = @"
/* get guest  */
select us_id, us_admin,
us_username, us_firstname, us_lastname,
isnull(us_email,'') us_email,
isnull(us_bugs_per_page,10) us_bugs_per_page,
isnull(us_forced_project,0) us_forced_project,
us_use_fckeditor,
us_enable_bug_list_popups,
og.*,
isnull(us_forced_project, 0 ) us_forced_project,
isnull(pu_permission_level, $dpl) pu_permission_level,
0 [project_admin]
from users
inner join orgs og on us_org = og_id
left outer join project_user_xref
    on pu_project = us_forced_project
    and pu_user = us_id
where us_username = 'guest'
and us_active = 1";

                    sql = sql.Replace("$dpl", ApplicationSettings.DefaultPermissionLevel.ToString());
                    dr = DbUtil.GetDataRow(sql);
                }

            // no previous session, no guest login allowed
            if (dr == null)
            {
                Util.WriteToLog("no previous session, no guest login allowed");
                response.Redirect(target);
                return;
            }
            else
            {
                this.User.SetFromDb(dr);
            }

            if (ApplicationSettings.WindowsAuthentication == 1)
            {
                AuthMethod = "windows";
            }
            else
            {
                AuthMethod = "plain";
            }

            if (cookie != null)
            {
                aspNetContext.Session["session_cookie"] = cookie.Value;
            }
            else
            {
                Util.WriteToLog("blanking cookie");
                aspNetContext.Session["session_cookie"] = "";
            }

            if (level == SecurityLevel.MustBeAdmin && !this.User.IsAdmin)
            {
                Util.WriteToLog("must be admin, redirecting");
                response.Redirect("~/Account/Login");
            }
            else if (level == SecurityLevel.AnyUserOkExceptGuest && this.User.IsGuest)
            {
                Util.WriteToLog("cant be guest, redirecting");
                response.Redirect("~/Account/Login");
            }
            else if (level == SecurityLevel.MustBeAdminOrProjectAdmin && !this.User.IsAdmin && !this.User.IsProjectAdmin)
            {
                Util.WriteToLog("must be project admin, redirecting");
                response.Redirect("~/Account/Login");
            }
        }

        public void CreateSession(HttpRequest request, HttpResponse response, int userid, string username, string ntlm)
        {
            // Generate a random session id
            // Don't use a regularly incrementing identity
            // column because that can be guessed.
            var guid = Guid.NewGuid().ToString();

            Util.WriteToLog("guid=" + guid);

            var sql = @"insert into sessions (se_id, se_user) values('$gu', $us)";
            sql = sql.Replace("$gu", guid);
            sql = sql.Replace("$us", Convert.ToString(userid));

            DbUtil.ExecuteNonQuery(sql);

            HttpContext.Current.Session[guid] = userid;

            var sAppPath = request.Url.AbsolutePath;
            sAppPath = sAppPath.Substring(0, sAppPath.LastIndexOf('/'));
            Util.WriteToLog("AppPath:" + sAppPath);

            response.Cookies.Set(new HttpCookie("se_id2", guid));
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
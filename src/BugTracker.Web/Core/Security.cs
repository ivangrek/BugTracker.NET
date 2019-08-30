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

    public class Security
    {
        public const int MustBeAdmin = 1;
        public const int AnyUserOk = 2;
        public const int AnyUserOkExceptGuest = 3;
        public const int MustBeAdminOrProjectAdmin = 4;
        public const int PermissionNone = 0;
        public const int PermissionReadonly = 1;
        public const int PermissionReporter = 3;
        public const int PermissionAll = 2;

        private static readonly string GotoForm = @"
<td nowrap valign=middle>
    <form style='margin: 0px; padding: 0px;' action=EditBug.aspx method=get>
        <input class=menubtn type=submit value='go to ID'>
        <input class=menuinput size=4 type=text class=txt name=id accesskey=g>
    </form>
</td>";

        public string AuthMethod = string.Empty;
        public HttpContext Context = null;

        public User User = new User();

        public void CheckSecurity(HttpContext aspNetContext, int level)
        {
            Util.SetContext(aspNetContext);
            var request = aspNetContext.Request;
            var response = aspNetContext.Response;
            var cookie = request.Cookies["se_id"];

            // This logic allows somebody to put a link in an email, like
            // EditBug.aspx?id=66
            // The user would click on the link, go to the logon page (default.aspx),
            // and then after logging in continue on to EditBug.aspx?id=66
            var originalUrl = request.ServerVariables["URL"].ToLower();
            var originalQuerystring = request.ServerVariables["QUERY_STRING"].ToLower();

            var target = "default.aspx";

            if (originalUrl.EndsWith("MBug.aspx")) target = "MLogin.aspx";

            target += "?url=" + originalUrl + "&qs=" + HttpUtility.UrlEncode(originalQuerystring);

            DataRow dr = null;

            if (cookie == null)
            {
                if (Util.GetSetting("AllowGuestWithoutLogin", "0") == "0")
                {
                    Util.WriteToLog("se_id cookie is null, so redirecting");
                    response.Redirect(target);
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
                sql = sql.Replace("$dpl", Util.GetSetting("DefaultPermissionLevel", "2"));
                dr = DbUtil.GetDataRow(sql);
            }

            if (dr == null)
                if (Util.GetSetting("AllowGuestWithoutLogin", "0") == "1")
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

                    sql = sql.Replace("$dpl", Util.GetSetting("DefaultPermissionLevel", "2"));
                    dr = DbUtil.GetDataRow(sql);
                }

            // no previous session, no guest login allowed
            if (dr == null)
            {
                Util.WriteToLog("no previous session, no guest login allowed");
                response.Redirect(target);
            }
            else
            {
                this.User.SetFromDb(dr);
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

            if (level == MustBeAdmin && !this.User.IsAdmin)
            {
                Util.WriteToLog("must be admin, redirecting");
                response.Redirect("default.aspx");
            }
            else if (level == AnyUserOkExceptGuest && this.User.IsGuest)
            {
                Util.WriteToLog("cant be guest, redirecting");
                response.Redirect("default.aspx");
            }
            else if (level == MustBeAdminOrProjectAdmin && !this.User.IsAdmin && !this.User.IsProjectAdmin)
            {
                Util.WriteToLog("must be project admin, redirecting");
                response.Redirect("default.aspx");
            }

            if (Util.GetSetting("WindowsAuthentication", "0") == "1")
                this.AuthMethod = "windows";
            else
                this.AuthMethod = "plain";
        }

        public static void CreateSession(HttpRequest request, HttpResponse response, int userid, string username, string ntlm)
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

            response.Cookies["se_id"].Value = guid;
            response.Cookies["se_id"].Path = sAppPath;
            response.Cookies["user"]["name"] = username;
            response.Cookies["user"]["NTLM"] = ntlm;
            response.Cookies["user"].Path = sAppPath;
            var dt = DateTime.Now;
            var ts = new TimeSpan(365, 0, 0, 0);
            response.Cookies["user"].Expires = dt.Add(ts);
        }

        public void WriteMenuItem(HttpResponse response,
            string thisLink, string menuItem, string href)
        {
            response.Write("<td class='menu_td'>");
            if (thisLink == menuItem)
                response.Write("<a href=" + href + "><span class='selected_menu_item warn'  style='margin-left:3px;'>" +
                               menuItem + "</span></a>");
            else
                response.Write("<a href=" + href + "><span class='menu_item warn' style='margin-left:3px;'>" +
                               menuItem + "</span></a>");
            response.Write("</td>");
        }

        public void WriteMenu(HttpResponse response, string thisLink)
        {
            // topmost visible HTML
            var customHeader = (string)Util.Context.Application["custom_header"];
            response.Write(customHeader);

            response.Write(@"
<span id=debug style='position:absolute;top:0;left:0;'></span>
<script>
function dbg(s)
{
	document.getElementById('debug').innerHTML += (s + '<br>')
}
function on_submit_search()
{
	el = document.getElementById('lucene_input')
	if (el.value == '')
	{
		alert('Enter the words you are search for.');
		el.focus()
		return false;
	}
	else
	{
		return true;
	}

}

</script>
<table border=0 width=100% cellpadding=0 cellspacing=0 class=menubar><tr>");

            // logo
            var logo = (string)Util.Context.Application["custom_logo"];
            response.Write(logo);

            response.Write("<td width=20>&nbsp;</td>");
            WriteMenuItem(response, thisLink, Util.GetSetting("PluralBugLabel", "bugs"), "Bugs.aspx");

            if (this.User.CanSearch) WriteMenuItem(response, thisLink, "search", "Search.aspx");

            if (Util.GetSetting("EnableWhatsNewPage", "0") == "1")
                WriteMenuItem(response, thisLink, "news", "ViewWhatsNew.aspx");

            if (!this.User.IsGuest) WriteMenuItem(response, thisLink, "queries", "Queries.aspx");

            if (this.User.IsAdmin || this.User.CanUseReports || this.User.CanEditReports)
                WriteMenuItem(response, thisLink, "reports", "Reports.aspx");

            if (Util.GetSetting("CustomMenuLinkLabel", "") != "")
                WriteMenuItem(response, thisLink,
                    Util.GetSetting("CustomMenuLinkLabel", ""),
                    Util.GetSetting("CustomMenuLinkUrl", ""));

            if (this.User.IsAdmin)
                WriteMenuItem(response, thisLink, "admin", "Admin.aspx");
            else if (this.User.IsProjectAdmin) WriteMenuItem(response, thisLink, "users", "Users.aspx");

            // go to

            response.Write(GotoForm);

            // search
            if (Util.GetSetting("EnableLucene", "1") == "1" && this.User.CanSearch)
            {
                var query = (string)HttpContext.Current.Session["query"];
                if (query == null) query = "";
                var searchForm = @"

<td nowrap valign=middle>
    <form style='margin: 0px; padding: 0px;' action=SearchText.aspx method=get onsubmit='return on_submit_search()'>
        <input class=menubtn type=submit value='search text'>
        <input class=menuinput  id=lucene_input size=24 type=text class=txt
        value='" + query.Replace("'", "") + @"' name=query accesskey=s>
        <a href=Content/lucene_syntax.html target=_blank style='font-size: 7pt;'>advanced</a>
    </form>
</td>";
                //context.Session["query"] = null;
                response.Write(searchForm);
            }

            response.Write("<td nowrap valign=middle>");
            if (this.User.IsGuest && Util.GetSetting("AllowGuestWithoutLogin", "0") == "1")
                response.Write("<span class=smallnote>using as<br>");
            else
                response.Write("<span class=smallnote>logged in as<br>");
            response.Write(this.User.Username);
            response.Write("</span></td>");

            if (this.AuthMethod == "plain")
            {
                if (this.User.IsGuest && Util.GetSetting("AllowGuestWithoutLogin", "0") == "1")
                    WriteMenuItem(response, thisLink, "login", "default.aspx");
                else
                    WriteMenuItem(response, thisLink, "logoff", "Logoff.aspx");
            }

            // for guest account, suppress display of "edit_self
            if (!this.User.IsGuest) WriteMenuItem(response, thisLink, "settings", "EditSelf.aspx");

            response.Write("<td valign=middle align=left'>");
            response.Write(
                "<a target=_blank href=Content/about.html><span class='menu_item' style='margin-left:3px;'>about</span></a></td>");
            response.Write("<td nowrap valign=middle>");
            response.Write(
                "<a target=_blank href=http://ifdefined.com/README.html><span class='menu_item' style='margin-left:3px;'>help</span></a></td>");

            response.Write("</tr></table><br>");
        }
    }
}
/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Controllers
{
    using BugTracker.Web.Core;
    using System;
    using System.Data;
    using System.Globalization;
    using System.Text;
    using System.Threading;
    using System.Web.Mvc;

    public class AspController : Controller
    {
        private readonly ISecurity security;

        public AspController(
            ISecurity security)
        {
            this.security = security;
        }

        [HttpGet]
        public void Ajax(int bugid)
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.AnyUserOk);

            // check permission
            var permissionLevel = Bug.GetBugPermissionLevel(/*Convert.ToInt32(*/bugid/*)*/, this.security);

            if (permissionLevel != SecurityPermissionLevel.PermissionNone)
            {
                Response.Write(@"
                    <style>
                        .cmt_text {
                            font-family: courier new;
                            font-size: 8pt;
                        }
                        
                        .pst {
                            font-size: 7pt;
                        }
                    </style>");

                using (var dsPosts = PrintBug.GetBugPosts(bugid, this.security.User.ExternalUser, false))
                {
                    var postCnt = PrintBug.WritePosts(
                        dsPosts,
                        System.Web.HttpContext.Current.Response,
                        bugid,
                        permissionLevel,
                        false, // write links
                        false, // images inline
                        false, // history inline
                        true, // internal posts
                        this.security.User);

                    // We can't unwrite what we wrote, but let's tell javascript to ignore it.
                    if (postCnt == 0)
                    {
                        Response.Write("<!--zeroposts-->");
                    }
                }
            }
            else
            {
                Response.Write(string.Empty);
            }
        }

        [HttpGet]
        public void Ajax2(string q)
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.AnyUserOk);

            // will this be too slow?
            // we could index on bg_short_desc and then do '$str%' rather than '%$str%'

            try
            {
                var sql = @"
                    select distinct top 10
                        bg_short_desc
                    from bugs
                    where bg_short_desc like '%$str%'
                    order by 1";

                // if you don't use permissions, comment out this line for speed?
                sql = Util.AlterSqlPerProjectPermissions(sql, this.security);

                var text = q ?? string.Empty;
                sql = sql.Replace("$str", text.Replace("'", "''"));

                using (var ds = DbUtil.GetDataSet(sql))
                {
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        Response.Write("<select id='suggest_select' class='suggest_select'	size=6 ");
                        Response.Write(" onclick='select_suggestion(this)' onkeydown='return suggest_sel_onkeydown(this, event)'>");
                        foreach (DataRow dr in ds.Tables[0].Rows)
                        {
                            Response.Write("<option>");
                            Response.Write(dr[0]);
                            Response.Write("</option>");
                        }

                        Response.Write("</select>");
                    }
                    else
                    {
                        Response.Write(string.Empty);
                    }
                }
            }
            catch
            {
                Response.Write(string.Empty);
            }
        }

        [HttpGet]
        public ActionResult Hello()
        {
            var currentCultureSeparator = new CultureInfo(Thread.CurrentThread.CurrentCulture.Name)
                .NumberFormat.NumberDecimalSeparator;

            var deSeparator = new CultureInfo("de-DE")
                .NumberFormat.NumberDecimalSeparator;

            return Content($@"Hello<br>{Thread.CurrentThread.CurrentCulture.Name}<br>CurrentCulture NumberDecimalSeparator: {currentCultureSeparator}<br>DE NumberDecimalSeparator: {deSeparator}");
        }

        [HttpGet]
        public ActionResult Install(string dbname)
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            if (!string.IsNullOrEmpty(dbname))
            {
                dbname = dbname.Replace("'", "''");

                try
                {
                    // don't allow lots of dbs to be created by somebody malicious
                    if (HttpContext.ApplicationInstance.Application["dbs"] == null)
                    {
                        HttpContext.ApplicationInstance.Application["dbs"] = 0;
                    }

                    var dbs = (int)HttpContext.ApplicationInstance.Application["dbs"];

                    if (dbs > 10)
                    {
                        return Content(string.Empty);
                    }

                    HttpContext.ApplicationInstance.Application["dbs"] = ++dbs;

                    using (DbUtil.GetSqlConnection())
                    { }

                    var sql = @"
                        use master
                        create database [$db]"
                        .Replace("$db", dbname);

                    DbUtil.ExecuteNonQuery(sql);

                    return Content("<font color=red><b>Database Created.</b></font>");
                }
                catch (Exception ex)
                {
                    return Content($"<font color='red'><b>{ex.Message}</b></font>");
                }
            }

            return View();
        }

        [HttpGet]
        public ActionResult Upgrade273To274()
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            var sql = "select us_username, us_id, us_password from users where len(us_password) < 32";

            var ds = DbUtil.GetDataSet(sql);
            var stringBuilder = new StringBuilder();

            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                Thread.Sleep(10); // give time for the random number to seed differently;

                var usUsername = (string)dr["us_username"];
                var usId = (int)dr["us_id"];
                var usPassword = (string)dr["us_password"];

                Util.UpdateUserPassword(usId, usPassword);

                stringBuilder.Append("encrypting " + usUsername + "<br>");
            }

            stringBuilder.Append("done encrypting unencrypted passwords");

            return Content(stringBuilder.ToString());
        }

        // TODO replace to bugs
        [HttpGet]
        public ActionResult Vote(string ses, int bugid)
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.AnyUserOk);

            if (!this.security.User.IsGuest)
            {
                if (ses != (string)Session["session_cookie"])
                {
                    return Content("session in URL doesn't match session cookie");
                }
            }

            var dv = (DataView)Session["bugs"];

            if (dv == null)
            {
                return Content(string.Empty);
            }

            var permissionLevel = Bug.GetBugPermissionLevel(bugid, this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                return Content(string.Empty);
            }

            for (var i = 0; i < dv.Count; i++)
            {
                if ((int)dv[i][1] == bugid)
                {
                    // treat it like a delta and update the cached vote count.
                    var vote = Convert.ToInt32(Util.SanitizeInteger(Request["vote"]));
                    var objVoteCount = HttpContext.ApplicationInstance.Application[Convert.ToString(bugid)];
                    var voteCount = 0;

                    if (objVoteCount != null)
                    {
                        voteCount = (int)objVoteCount;
                    }

                    voteCount += vote;

                    HttpContext.ApplicationInstance.Application[Convert.ToString(bugid)] = voteCount;

                    // now treat it more like a boolean
                    if (vote == -1)
                    {
                        vote = 0;
                    }

                    dv[i]["$VOTE"] = vote;

                    var sql = @"
                        if not exists (select bu_bug from bug_user where bu_bug = $bg and bu_user = $us)
                        insert into bug_user (bu_bug, bu_user, bu_flag, bu_seen, bu_vote) values($bg, $us, 0, 0, 1) 
                        update bug_user set bu_vote = $vote, bu_vote_datetime = getdate() where bu_bug = $bg and bu_user = $us and bu_vote <> $vote"
                        .Replace("$vote", Convert.ToString(vote))
                        .Replace("$bg", Convert.ToString(bugid))
                        .Replace("$us", Convert.ToString(this.security.User.Usid));

                    DbUtil.ExecuteNonQuery(sql);

                    break;
                }
            }

            return Content("OK");
        }

        // TODO replace to bugs
        [HttpGet]
        public ActionResult Seen(string ses, int bugid, int seen)
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.AnyUserOk);

            if (!this.security.User.IsGuest)
            {
                if (ses != (string)Session["session_cookie"])
                {
                    return Content("session in URL doesn't match session cookie");
                }
            }

            var dv = (DataView)Session["bugs"];

            if (dv == null)
            {
                return Content(string.Empty);
            }

            var permissionLevel = Bug.GetBugPermissionLevel(bugid, this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                return Content(string.Empty);
            }

            for (var i = 0; i < dv.Count; i++)
            {
                if ((int)dv[i][1] == bugid)
                {
                    dv[i]["$SEEN"] = seen;

                    var sql = @"
                        if not exists (select bu_bug from bug_user where bu_bug = $bg and bu_user = $us)
                        insert into bug_user (bu_bug, bu_user, bu_flag, bu_seen, bu_vote) values($bg, $us, 0, 1, 0) 
                        update bug_user set bu_seen = $seen, bu_seen_datetime = getdate() where bu_bug = $bg and bu_user = $us and bu_seen <> $seen"
                    .Replace("$seen", Convert.ToString(seen))
                    .Replace("$bg", Convert.ToString(bugid))
                    .Replace("$us", Convert.ToString(this.security.User.Usid));

                    DbUtil.ExecuteNonQuery(sql);

                    break;
                }
            }

            return Content("OK");
        }

        // TODO replace to bugs
        [HttpGet]
        public ActionResult Flag(string ses, int bugid, int flag)
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.AnyUserOk);

            if (!this.security.User.IsGuest)
            {
                if (ses != (string)Session["session_cookie"])
                {
                    return Content("session in URL doesn't match session cookie");
                }
            }

            var dv = (DataView)Session["bugs"];

            if (dv == null)
            {
                return Content(string.Empty);
            }

            var permissionLevel = Bug.GetBugPermissionLevel(bugid, this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                return Content(string.Empty);
            }

            for (var i = 0; i < dv.Count; i++)
            {
                if ((int)dv[i][1] == bugid)
                {
                    dv[i]["$FLAG"] = flag;

                    var sql = @"
                        if not exists (select bu_bug from bug_user where bu_bug = $bg and bu_user = $us)
                        insert into bug_user (bu_bug, bu_user, bu_flag, bu_seen, bu_vote) values($bg, $us, 1, 0, 0) 
                        update bug_user set bu_flag = $fl, bu_flag_datetime = getdate() where bu_bug = $bg and bu_user = $us and bu_flag <> $fl"
                    .Replace("$bg", Convert.ToString(bugid))
                    .Replace("$us", Convert.ToString(this.security.User.Usid))
                    .Replace("$fl", Convert.ToString(flag));

                    DbUtil.ExecuteNonQuery(sql);
                    break;
                }
            }

            return Content("OK");
        }
    }
}
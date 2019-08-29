/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Data;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class vote : Page
    {
        public Security security;
        public string sql;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            if (!this.security.user.is_guest)
                if (Request.QueryString["ses"] != (string) Session["session_cookie"])
                {
                    Response.Write("session in URL doesn't match session cookie");
                    Response.End();
                }

            var dv = (DataView) Session["bugs"];
            if (dv == null) Response.End();

            var bugid = Convert.ToInt32(Util.sanitize_integer(Request["bugid"]));

            var permission_level = Bug.get_bug_permission_level(bugid, this.security);
            if (permission_level == Security.PERMISSION_NONE) Response.End();

            for (var i = 0; i < dv.Count; i++)
                if ((int) dv[i][1] == bugid)
                {
                    // treat it like a delta and update the cached vote count.
                    var vote = Convert.ToInt32(Util.sanitize_integer(Request["vote"]));
                    var obj_vote_count = Application[Convert.ToString(bugid)];
                    var vote_count = 0;

                    if (obj_vote_count != null)
                        vote_count = (int) obj_vote_count;

                    vote_count += vote;

                    Application[Convert.ToString(bugid)] = vote_count;

                    // now treat it more like a boolean
                    if (vote == -1)
                        vote = 0;

                    dv[i]["$VOTE"] = vote;
                    this.sql = @"
if not exists (select bu_bug from bug_user where bu_bug = $bg and bu_user = $us)
	insert into bug_user (bu_bug, bu_user, bu_flag, bu_seen, bu_vote) values($bg, $us, 0, 0, 1) 
update bug_user set bu_vote = $vote, bu_vote_datetime = getdate() where bu_bug = $bg and bu_user = $us and bu_vote <> $vote";

                    this.sql = this.sql.Replace("$vote", Convert.ToString(vote));
                    this.sql = this.sql.Replace("$bg", Convert.ToString(bugid));
                    this.sql = this.sql.Replace("$us", Convert.ToString(this.security.user.usid));

                    DbUtil.execute_nonquery(this.sql);

                    break;
                }
        }
    }
}
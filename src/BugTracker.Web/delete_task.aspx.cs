/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class delete_task : Page
    {
        public Security security;
        public string sql;

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();

            this.security.check_security(HttpContext.Current, Security.MUST_BE_ADMIN);

            if (Request.QueryString["ses"] != (string) Session["session_cookie"])
            {
                Response.Write("session in URL doesn't match session cookie");
                Response.End();
            }

            var string_bugid = Util.sanitize_integer(Request["bugid"]);
            var bugid = Convert.ToInt32(string_bugid);

            var permission_level = Bug.get_bug_permission_level(bugid, this.security);

            if (permission_level != Security.PERMISSION_ALL)
            {
                Response.Write("You are not allowed to edit this item");
                Response.End();
            }

            var string_tsk_id = Util.sanitize_integer(Request["id"]);
            var tsk_id = Convert.ToInt32(string_tsk_id);

            if (IsPostBack)
            {
                // do delete here

                this.sql = @"delete bug_tasks where tsk_id = $tsk_id and tsk_bug = $bugid";
                this.sql = this.sql.Replace("$tsk_id", string_tsk_id);
                this.sql = this.sql.Replace("$bugid", string_bugid);
                DbUtil.execute_nonquery(this.sql);
                Response.Redirect("tasks.aspx?bugid=" + string_bugid);
            }
            else
            {
                Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                            + "delete task";

                this.back_href.HRef = "tasks.aspx?bugid=" + string_bugid;

                this.sql = @"select tsk_description from bug_tasks where tsk_id = $tsk_id and tsk_bug = $bugid";
                this.sql = this.sql.Replace("$tsk_id", string_tsk_id);
                this.sql = this.sql.Replace("$bugid", string_bugid);

                var dr = DbUtil.get_datarow(this.sql);

                this.confirm_href.InnerText = "confirm delete of task: " + Convert.ToString(dr["tsk_description"]);
            }
        }
    }
}
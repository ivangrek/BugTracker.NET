/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Web.UI;
    using Core;

    public partial class DeleteTask : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public ISecurity Security { get; set; }

        public string Sql;

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.MustBeAdmin);

            if (Request.QueryString["ses"] != (string) Session["session_cookie"])
            {
                Response.Write("session in URL doesn't match session cookie");
                Response.End();
            }

            var stringBugid = Util.SanitizeInteger(Request["bugid"]);
            var bugid = Convert.ToInt32(stringBugid);

            var permissionLevel = Bug.GetBugPermissionLevel(bugid, Security);

            if (permissionLevel != SecurityPermissionLevel.PermissionAll)
            {
                Response.Write("You are not allowed to edit this item");
                Response.End();
            }

            var stringTskId = Util.SanitizeInteger(Request["id"]);
            var tskId = Convert.ToInt32(stringTskId);

            if (IsPostBack)
            {
                // do delete here

                this.Sql = @"delete bug_tasks where tsk_id = $tsk_id and tsk_bug = $bugid";
                this.Sql = this.Sql.Replace("$tsk_id", stringTskId);
                this.Sql = this.Sql.Replace("$bugid", stringBugid);
                DbUtil.ExecuteNonQuery(this.Sql);
                Response.Redirect("Tasks.aspx?bugid=" + stringBugid);
            }
            else
            {
                Page.Title = $"{ApplicationSettings.AppTitle} - delete task";

                this.back_href.HRef = "Tasks.aspx?bugid=" + stringBugid;

                this.Sql = @"select tsk_description from bug_tasks where tsk_id = $tsk_id and tsk_bug = $bugid";
                this.Sql = this.Sql.Replace("$tsk_id", stringTskId);
                this.Sql = this.Sql.Replace("$bugid", stringBugid);

                var dr = DbUtil.GetDataRow(this.Sql);

                this.confirm_href.InnerText = "confirm delete of task: " + Convert.ToString(dr["tsk_description"]);
            }
        }
    }
}
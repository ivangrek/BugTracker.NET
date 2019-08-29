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

    public partial class print_bugs2 : Page
    {
        public DataSet ds;
        public DataView dv;
        public bool history_inline;
        public bool images_inline;

        public Security security;
        public string sql;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "print " +
                                                                        Util.get_setting("PluralBugLabel", "bugs");

            // are we doing the query to get the bugs or are we using the cached dataview?
            var qu_id_string = Request.QueryString["qu_id"];

            if (qu_id_string != null)
            {
                // use sql specified in query string
                var qu_id = Convert.ToInt32(qu_id_string);
                this.sql = @"select qu_sql from queries where qu_id = $1";
                this.sql = this.sql.Replace("$1", qu_id_string);
                var bug_sql = (string) DbUtil.execute_scalar(this.sql);

                // replace magic variables
                bug_sql = bug_sql.Replace("$ME", Convert.ToString(this.security.user.usid));
                bug_sql = Util.alter_sql_per_project_permissions(bug_sql, this.security);

                // all we really need is the bugid, but let's do the same query as print_bugs.aspx
                this.ds = DbUtil.get_dataset(bug_sql);
            }
            else
            {
                this.dv = (DataView) Session["bugs"];
            }

            var cookie = Request.Cookies["images_inline"];
            if (cookie == null || cookie.Value == "0")
                this.images_inline = false;
            else
                this.images_inline = true;

            cookie = Request.Cookies["history_inline"];
            if (cookie == null || cookie.Value == "0")
                this.history_inline = false;
            else
                this.history_inline = true;
        }
    }
}
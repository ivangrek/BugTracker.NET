/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Bugs
{
    using System;
    using System.Data;
    using System.Web.UI;
    using Core;

    public partial class Print2 : Page
    {
        public DataSet Ds;
        public DataView Dv;
        public bool HistoryInline;
        public bool ImagesInline;
        public string Sql;

        public Security Security { get; set; }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            var security = new Security();

            security.CheckSecurity(Security.AnyUserOk);

            Security = security;

            Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - print " +
                                                                        Util.GetSetting("PluralBugLabel", "bugs");

            // are we doing the query to get the bugs or are we using the cached dataview?
            var quIdString = Request.QueryString["qu_id"];

            if (quIdString != null)
            {
                // use sql specified in query string
                var quId = Convert.ToInt32(quIdString);
                this.Sql = @"select qu_sql from queries where qu_id = $1";
                this.Sql = this.Sql.Replace("$1", quIdString);
                var bugSql = (string) DbUtil.ExecuteScalar(this.Sql);

                // replace magic variables
                bugSql = bugSql.Replace("$ME", Convert.ToString(security.User.Usid));
                bugSql = Util.AlterSqlPerProjectPermissions(bugSql, security);

                // all we really need is the bugid, but let's do the same query as Bugs/Print.aspx
                this.Ds = DbUtil.GetDataSet(bugSql);
            }
            else
            {
                this.Dv = (DataView) Session["bugs"];
            }

            var cookie = Request.Cookies["images_inline"];
            if (cookie == null || cookie.Value == "0")
                this.ImagesInline = false;
            else
                this.ImagesInline = true;

            cookie = Request.Cookies["history_inline"];
            if (cookie == null || cookie.Value == "0")
                this.HistoryInline = false;
            else
                this.HistoryInline = true;
        }
    }
}
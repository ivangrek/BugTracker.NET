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

    public partial class DeleteOrg : Page
    {
        public Security Security;
        public string Sql;

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            this.Security = new Security();
            this.Security.CheckSecurity(HttpContext.Current, Security.MustBeAdmin);

            if (IsPostBack)
            {
                // do delete here
                this.Sql = @"delete orgs where og_id = $1";
                this.Sql = this.Sql.Replace("$1", Util.SanitizeInteger(this.row_id.Value));
                DbUtil.ExecuteNonQuery(this.Sql);
                Server.Transfer("Orgs.aspx");
            }
            else
            {
                Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - "
                                                                            + "delete organization";

                var id = Util.SanitizeInteger(Request["id"]);

                this.Sql = @"declare @cnt int
			select @cnt = count(1) from users where us_org = $1;
			select @cnt = @cnt + count(1) from queries where qu_org = $1;
			select @cnt = @cnt + count(1) from bugs where bg_org = $1;
			select og_name, @cnt [cnt] from orgs where og_id = $1";
                this.Sql = this.Sql.Replace("$1", id);

                var dr = DbUtil.GetDataRow(this.Sql);

                if ((int) dr["cnt"] > 0)
                {
                    Response.Write("You can't delete organization \""
                                   + Convert.ToString(dr["og_name"])
                                   + "\" because some bugs, users, queries still reference it.");
                    Response.End();
                }
                else
                {
                    this.confirm_href.InnerText = "confirm delete of \""
                                                  + Convert.ToString(dr["og_name"])
                                                  + "\"";

                    this.row_id.Value = id;
                }
            }
        }
    }
}
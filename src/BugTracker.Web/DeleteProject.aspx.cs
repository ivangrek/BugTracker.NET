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

    public partial class DeleteProject : Page
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
                this.Sql = @"delete projects where pj_id = $1";
                this.Sql = this.Sql.Replace("$1", Util.SanitizeInteger(this.row_id.Value));
                DbUtil.ExecuteNonQuery(this.Sql);
                Server.Transfer("Projects.aspx");
            }
            else
            {
                Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - "
                                                                            + "delete project";

                var id = Util.SanitizeInteger(Request["id"]);

                this.Sql = @"declare @cnt int
			select @cnt = count(1) from bugs where bg_project = $1
			select pj_name, @cnt [cnt] from projects where pj_id = $1";
                this.Sql = this.Sql.Replace("$1", id);

                var dr = DbUtil.GetDataRow(this.Sql);

                if ((int) dr["cnt"] > 0)
                {
                    Response.Write("You can't delete project \""
                                   + Convert.ToString(dr["pj_name"])
                                   + "\" because some bugs still reference it.");
                    Response.End();
                }
                else
                {
                    this.confirm_href.InnerText = "confirm delete of \""
                                                  + Convert.ToString(dr["pj_name"])
                                                  + "\"";

                    this.row_id.Value = id;
                }
            }
        }
    }
}
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

    public partial class delete_category : Page
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

            if (IsPostBack)
            {
                this.sql = @"delete categories where ct_id = $1";
                this.sql = this.sql.Replace("$1", Util.sanitize_integer(this.row_id.Value));
                DbUtil.execute_nonquery(this.sql);
                Server.Transfer("categories.aspx");
            }
            else
            {
                Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                            + "delete category";

                var id = Util.sanitize_integer(Request["id"]);

                this.sql = @"declare @cnt int
			select @cnt = count(1) from bugs where bg_category = $1
			select ct_name, @cnt [cnt] from categories where ct_id = $1";
                this.sql = this.sql.Replace("$1", id);

                var dr = DbUtil.get_datarow(this.sql);

                if ((int) dr["cnt"] > 0)
                {
                    Response.Write("You can't delete category \""
                                   + Convert.ToString(dr["ct_name"])
                                   + "\" because some bugs still reference it.");
                    Response.End();
                }
                else
                {
                    this.confirm_href.InnerText = "confirm delete of \""
                                                  + Convert.ToString(dr["ct_name"])
                                                  + "\"";

                    this.row_id.Value = id;
                }
            }
        }
    }
}
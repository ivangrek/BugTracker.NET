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

    public partial class delete_query : Page
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

            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            if (IsPostBack)
            {
                // do delete here
                this.sql = @"delete queries where qu_id = $1";
                this.sql = this.sql.Replace("$1", Util.sanitize_integer(this.row_id.Value));
                DbUtil.execute_nonquery(this.sql);
                Server.Transfer("queries.aspx");
            }
            else
            {
                Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                            + "delete query";

                var id = Util.sanitize_integer(Request["id"]);

                this.sql = @"select qu_desc, isnull(qu_user,0) qu_user from queries where qu_id = $1";
                this.sql = this.sql.Replace("$1", id);

                var dr = DbUtil.get_datarow(this.sql);

                if ((int) dr["qu_user"] != this.security.user.usid)
                {
                    if (this.security.user.is_admin || this.security.user.can_edit_sql)
                    {
                        // can do anything
                    }
                    else
                    {
                        Response.Write("You are not allowed to delete this item");
                        Response.End();
                    }
                }

                this.confirm_href.InnerText = "confirm delete of query: "
                                              + Convert.ToString(dr["qu_desc"]);

                this.row_id.Value = id;
            }
        }
    }
}
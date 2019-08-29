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

    public partial class delete_customfield : Page
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

            Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "delete custom field";

            if (IsPostBack)
            {
                // do delete here

                this.sql = @"select sc.name [column_name], df.name [default_constraint_name]
			from syscolumns sc
			inner join sysobjects so on sc.id = so.id
			left outer join sysobjects df on df.id = sc.cdefault
			where so.name = 'bugs'
			and sc.colorder = $id";

                this.sql = this.sql.Replace("$id", Util.sanitize_integer(this.row_id.Value));
                var dr = DbUtil.get_datarow(this.sql);

                // if there is a default, delete it
                if (dr["default_constraint_name"].ToString() != "")
                {
                    this.sql = @"alter table bugs drop constraint [$df]";
                    this.sql = this.sql.Replace("$df", (string) dr["default_constraint_name"]);
                    DbUtil.execute_nonquery(this.sql);
                }

                // delete column itself
                this.sql = @"
alter table orgs drop column [og_$nm_field_permission_level]
alter table bugs drop column [$nm]";

                this.sql = this.sql.Replace("$nm", (string) dr["column_name"]);
                DbUtil.execute_nonquery(this.sql);

                //delete row from custom column table
                this.sql = @"delete from custom_col_metadata
		where ccm_colorder = $num";
                this.sql = this.sql.Replace("$num", Util.sanitize_integer(this.row_id.Value));

                Application["custom_columns_dataset"] = null;
                DbUtil.execute_nonquery(this.sql);

                Response.Redirect("customfields.aspx");
            }
            else
            {
                var id = Util.sanitize_integer(Request["id"]);

                this.sql = @"select sc.name
			from syscolumns sc
			inner join sysobjects so on sc.id = so.id
			left outer join sysobjects df on df.id = sc.cdefault
			where so.name = 'bugs'
			and sc.colorder = $id";

                this.sql = this.sql.Replace("$id", id);
                var dr = DbUtil.get_datarow(this.sql);

                this.confirm_href.InnerText = "confirm delete of \""
                                              + Convert.ToString(dr["name"])
                                              + "\"";

                this.row_id.Value = id;
            }
        }
    }
}
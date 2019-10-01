/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Administration.CustomFields
{
    using System;
    using System.Web.UI;
    using BugTracker.Web.Core.Controls;
    using Core;

    public partial class Delete : Page
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

            MainMenu.SelectedItem = MainMenuSections.Administration;

            Page.Title = $"{ApplicationSettings.AppTitle} - delete custom field";

            if (IsPostBack)
            {
                // do delete here

                this.Sql = @"select sc.name [column_name], df.name [default_constraint_name]
            from syscolumns sc
            inner join sysobjects so on sc.id = so.id
            left outer join sysobjects df on df.id = sc.cdefault
            where so.name = 'bugs'
            and sc.colorder = $id";

                this.Sql = this.Sql.Replace("$id", Util.SanitizeInteger(this.row_id.Value));
                var dr = DbUtil.GetDataRow(this.Sql);

                // if there is a default, delete it
                if (dr["default_constraint_name"].ToString() != "")
                {
                    this.Sql = @"alter table bugs drop constraint [$df]";
                    this.Sql = this.Sql.Replace("$df", (string) dr["default_constraint_name"]);
                    DbUtil.ExecuteNonQuery(this.Sql);
                }

                // delete column itself
                this.Sql = @"
alter table orgs drop column [og_$nm_field_permission_level]
alter table bugs drop column [$nm]";

                this.Sql = this.Sql.Replace("$nm", (string) dr["column_name"]);
                DbUtil.ExecuteNonQuery(this.Sql);

                //delete row from custom column table
                this.Sql = @"delete from custom_col_metadata
        where ccm_colorder = $num";
                this.Sql = this.Sql.Replace("$num", Util.SanitizeInteger(this.row_id.Value));

                Application["custom_columns_dataset"] = null;
                DbUtil.ExecuteNonQuery(this.Sql);

                Response.Redirect("~/Administration/CustomFields/List.aspx");
            }
            else
            {
                var id = Util.SanitizeInteger(Request["id"]);

                this.Sql = @"select sc.name
            from syscolumns sc
            inner join sysobjects so on sc.id = so.id
            left outer join sysobjects df on df.id = sc.cdefault
            where so.name = 'bugs'
            and sc.colorder = $id";

                this.Sql = this.Sql.Replace("$id", id);
                var dr = DbUtil.GetDataRow(this.Sql);

                this.confirm_href.InnerText = "confirm delete of \""
                                              + Convert.ToString(dr["name"])
                                              + "\"";

                this.row_id.Value = id;
            }
        }
    }
}
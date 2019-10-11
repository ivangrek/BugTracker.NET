/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Administration.Projects
{
    using System;
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using BugTracker.Web.Core.Controls;
    using Core;

    public partial class EditUserPermissions2 : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public ISecurity Security { get; set; }

        protected string Sql {get; set; }

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.MustBeAdmin);

            MainMenu.SelectedItem = MainMenuSections.Administration;

            Page.Title = $"{ApplicationSettings.AppTitle} - edit project per-user permissions";

            if (!IsPostBack)
            {
                var projectIdString = Util.SanitizeInteger(Request["id"]);

                if (Request["projects"] != null)
                {
                    this.back_href.InnerText = "back to projects";
                    this.back_href.HRef = ResolveUrl("~/Admin/Projects/List.aspx");
                }
                else
                {
                    this.back_href.InnerText = "back to project";
                    this.back_href.HRef = ResolveUrl($"~/Admin/Projects/Edit.aspx?id={projectIdString}");
                }

                this.Sql = @"Select us_username, us_id, isnull(pu_permission_level,$dpl) [pu_permission_level]
            from users
            left outer join project_user_xref on pu_user = us_id
            and pu_project = $pj
            order by us_username;
            select pj_name from projects where pj_id = $pj;";

                this.Sql = this.Sql.Replace("$pj", projectIdString);
                this.Sql = this.Sql.Replace("$dpl", ApplicationSettings.DefaultPermissionLevel.ToString());

                var ds = DbUtil.GetDataSet(this.Sql);

                this.MyDataGrid.DataSource = ds.Tables[0].DefaultView;
                this.MyDataGrid.DataBind();

                Page.Title = "Permissions for " + (string) ds.Tables[1].Rows[0][0];
            }
            else
            {
                on_update();
            }
        }

        public void on_update()
        {
            // now update all the recs
            var sqlBatch = "";
            RadioButton rb;
            string permissionLevel;

            foreach (DataGridItem dgi in this.MyDataGrid.Items)
            {
                this.Sql = @" if exists (select * from project_user_xref where pu_user = $us and pu_project = $pj)
                    update project_user_xref set pu_permission_level = $pu
                    where pu_user = $us and pu_project = $pj
                 else
                    insert into project_user_xref (pu_user, pu_project, pu_permission_level)
                    values ($us, $pj, $pu); ";

                this.Sql = this.Sql.Replace("$pj", Util.SanitizeInteger(Request["id"]));
                this.Sql = this.Sql.Replace("$us", Convert.ToString(dgi.Cells[1].Text));

                rb = (RadioButton) dgi.FindControl("none");
                if (rb.Checked)
                {
                    permissionLevel = "0";
                }
                else
                {
                    rb = (RadioButton) dgi.FindControl("readonly");
                    if (rb.Checked)
                    {
                        permissionLevel = "1";
                    }
                    else
                    {
                        rb = (RadioButton) dgi.FindControl("reporter");
                        if (rb.Checked)
                            permissionLevel = "3";
                        else
                            permissionLevel = "2";
                    }
                }

                this.Sql = this.Sql.Replace("$pu", permissionLevel);

                // add to the batch
                sqlBatch += this.Sql;
            }

            DbUtil.ExecuteNonQuery(sqlBatch);
            this.msg.InnerText = "Permissions have been updated.";
        }
    }
}
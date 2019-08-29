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
    using System.Web.UI.WebControls;
    using Core;

    public partial class edit_user_permissions2 : Page
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
                                                                        + "edit project per-user permissions";

            if (!IsPostBack)
            {
                var project_id_string = Util.sanitize_integer(Request["id"]);

                if (Request["projects"] != null)
                {
                    this.back_href.InnerText = "back to projects";
                    this.back_href.HRef = "projects.aspx";
                }
                else
                {
                    this.back_href.InnerText = "back to project";
                    this.back_href.HRef = "edit_project.aspx?id=" + project_id_string;
                }

                this.sql = @"Select us_username, us_id, isnull(pu_permission_level,$dpl) [pu_permission_level]
			from users
			left outer join project_user_xref on pu_user = us_id
			and pu_project = $pj
			order by us_username;
			select pj_name from projects where pj_id = $pj;";

                this.sql = this.sql.Replace("$pj", project_id_string);
                this.sql = this.sql.Replace("$dpl", Util.get_setting("DefaultPermissionLevel", "2"));

                var ds = DbUtil.get_dataset(this.sql);

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
            var sql_batch = "";
            RadioButton rb;
            string permission_level;

            foreach (DataGridItem dgi in this.MyDataGrid.Items)
            {
                this.sql = @" if exists (select * from project_user_xref where pu_user = $us and pu_project = $pj)
		            update project_user_xref set pu_permission_level = $pu
		            where pu_user = $us and pu_project = $pj
		         else
		            insert into project_user_xref (pu_user, pu_project, pu_permission_level)
		            values ($us, $pj, $pu); ";

                this.sql = this.sql.Replace("$pj", Util.sanitize_integer(Request["id"]));
                this.sql = this.sql.Replace("$us", Convert.ToString(dgi.Cells[1].Text));

                rb = (RadioButton) dgi.FindControl("none");
                if (rb.Checked)
                {
                    permission_level = "0";
                }
                else
                {
                    rb = (RadioButton) dgi.FindControl("readonly");
                    if (rb.Checked)
                    {
                        permission_level = "1";
                    }
                    else
                    {
                        rb = (RadioButton) dgi.FindControl("reporter");
                        if (rb.Checked)
                            permission_level = "3";
                        else
                            permission_level = "2";
                    }
                }

                this.sql = this.sql.Replace("$pu", permission_level);

                // add to the batch
                sql_batch += this.sql;
            }

            DbUtil.execute_nonquery(sql_batch);
            this.msg.InnerText = "Permissions have been updated.";
        }
    }
}
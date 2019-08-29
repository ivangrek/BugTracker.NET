/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Collections;
    using System.Data;
    using System.IO;
    using System.Web;
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using Core;

    public partial class backup_db : Page
    {
        public string app_data_folder;
        public Security security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.MUST_BE_ADMIN);

            Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "backup db";

            this.app_data_folder = HttpContext.Current.Server.MapPath(null);
            this.app_data_folder += "\\App_Data\\";

            if (!IsPostBack) get_files();
        }

        public void get_files()
        {
            var backup_files = Directory.GetFiles(this.app_data_folder, "*.bak");

            if (backup_files.Length == 0)
            {
                this.MyDataGrid.Visible = false;
                return;
            }

            this.MyDataGrid.Visible = true;

            // sort the files
            var list = new ArrayList();
            list.AddRange(backup_files);
            list.Sort();

            var dt = new DataTable();
            DataRow dr;

            dt.Columns.Add(new DataColumn("file", typeof(string)));
            dt.Columns.Add(new DataColumn("url", typeof(string)));

            for (var i = 0; i < list.Count; i++)
            {
                dr = dt.NewRow();

                var just_file = Path.GetFileName((string) list[i]);
                dr[0] = just_file;
                dr[1] = "download_file.aspx?which=backup&filename=" + just_file;

                dt.Rows.Add(dr);
            }

            var dv = new DataView(dt);

            this.MyDataGrid.DataSource = dv;
            this.MyDataGrid.DataBind();
        }

        public void on_backup(object sender, EventArgs e)
        {
            var date = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var db = (string) DbUtil.execute_scalar("select db_name()");
            var backup_file = this.app_data_folder + "db_backup_" + date + ".bak";
            var sql = "backup database " + db + " to disk = '" + backup_file + "'";
            DbUtil.execute_nonquery(sql);
            get_files();
        }

        public void my_button_click(object sender, DataGridCommandEventArgs e)
        {
            if (e.CommandName == "dlt")
            {
                var i = e.Item.ItemIndex;
                var file = this.MyDataGrid.Items[i].Cells[0].Text;
                File.Delete(this.app_data_folder + file);
                get_files();
            }
        }
    }
}
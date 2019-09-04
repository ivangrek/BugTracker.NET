/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Administration
{
    using System;
    using System.Collections;
    using System.Data;
    using System.IO;
    using System.Web;
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using Core;

    public partial class ManageLogs : Page
    {
        public string AppDataFolder;
        public Security Security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            this.Security = new Security();
            this.Security.CheckSecurity(HttpContext.Current, Security.MustBeAdmin);

            Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - manage logs";

            this.AppDataFolder = HttpContext.Current.Server.MapPath("~/");
            this.AppDataFolder += "\\App_Data\\logs\\";

            if (!IsPostBack) get_files();
        }

        public void get_files()
        {
            var backupFiles = Directory.GetFiles(this.AppDataFolder, "*.txt");

            if (backupFiles.Length == 0)
            {
                this.MyDataGrid.Visible = false;
                return;
            }

            this.MyDataGrid.Visible = true;

            // sort the files
            var list = new ArrayList();
            list.AddRange(backupFiles);
            list.Sort();

            var dt = new DataTable();
            DataRow dr;

            dt.Columns.Add(new DataColumn("file", typeof(string)));
            dt.Columns.Add(new DataColumn("url", typeof(string)));

            for (var i = list.Count - 1; i != -1; i--)
            {
                dr = dt.NewRow();

                var justFile = Path.GetFileName((string) list[i]);
                dr[0] = justFile;
                dr[1] = ResolveUrl($"~/Administration/DownloadFile.aspx?which=log&filename={justFile}");

                dt.Rows.Add(dr);
            }

            var dv = new DataView(dt);

            this.MyDataGrid.DataSource = dv;
            this.MyDataGrid.DataBind();
        }

        public void my_button_click(object sender, DataGridCommandEventArgs e)
        {
            if (e.CommandName == "dlt")
            {
                var i = e.Item.ItemIndex;
                var file = this.MyDataGrid.Items[i].Cells[0].Text;
                File.Delete(this.AppDataFolder + file);
                get_files();
            }
        }
    }
}
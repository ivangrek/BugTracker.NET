/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.IO;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class EditCustomHtml : Page
    {
        public Security Security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            this.Security = new Security();
            this.Security.CheckSecurity(HttpContext.Current, Security.MustBeAdmin);

            Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "edit web config";

            var whichFile = "";
            var fileName = "";

            if (!IsPostBack)
            {
                whichFile = Request["which"];

                // default to footer
                if (string.IsNullOrEmpty(whichFile)) whichFile = "footer";

                fileName = get_file_name(whichFile);
                this.msg.InnerHtml = "&nbsp;";
            }
            else
            {
                whichFile = this.which.Value;

                if (string.IsNullOrEmpty(whichFile)) Response.End();

                fileName = get_file_name(whichFile);

                if (fileName == "")
                    Response.End();

                // save to disk
                var path = HttpContext.Current.Server.MapPath(null);
                path += "\\Content\\custom\\";

                var sw = File.CreateText(path + fileName);
                sw.Write(this.myedit.Value);
                sw.Close();
                sw.Dispose();

                // save in Application (memory)
                Application[Path.GetFileNameWithoutExtension(fileName)] = this.myedit.Value;

                this.msg.InnerHtml = fileName + " was saved.";
            }

            load_file_into_control(fileName);

            this.which.Value = whichFile;
        }

        public void load_file_into_control(string fileName)
        {
            var path = HttpContext.Current.Server.MapPath(null);
            path += "\\Content\\custom\\" + fileName;

            var sr = File.OpenText(path);
            this.myedit.Value = sr.ReadToEnd();
            sr.Close();
            sr.Dispose();
        }

        public string get_file_name(string whichFile)
        {
            var fileName = "";

            if (whichFile == "css")
                fileName = "btnet_custom.css";
            else if (whichFile == "footer")
                fileName = "custom_footer.html";
            else if (whichFile == "header")
                fileName = "custom_header.html";
            else if (whichFile == "logo")
                fileName = "custom_logo.html";
            else if (whichFile == "welcome") fileName = "custom_welcome.html";

            return fileName;
        }
    }
}
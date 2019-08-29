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

    public partial class edit_custom_html : Page
    {
        public Security security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.MUST_BE_ADMIN);

            Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "edit web config";

            var which_file = "";
            var file_name = "";

            if (!IsPostBack)
            {
                which_file = Request["which"];

                // default to footer
                if (string.IsNullOrEmpty(which_file)) which_file = "footer";

                file_name = get_file_name(which_file);
                this.msg.InnerHtml = "&nbsp;";
            }
            else
            {
                which_file = this.which.Value;

                if (string.IsNullOrEmpty(which_file)) Response.End();

                file_name = get_file_name(which_file);

                if (file_name == "")
                    Response.End();

                // save to disk
                var path = HttpContext.Current.Server.MapPath(null);
                path += "\\custom\\";

                var sw = File.CreateText(path + file_name);
                sw.Write(this.myedit.Value);
                sw.Close();
                sw.Dispose();

                // save in Application (memory)
                Application[Path.GetFileNameWithoutExtension(file_name)] = this.myedit.Value;

                this.msg.InnerHtml = file_name + " was saved.";
            }

            load_file_into_control(file_name);

            this.which.Value = which_file;
        }

        public void load_file_into_control(string file_name)
        {
            var path = HttpContext.Current.Server.MapPath(null);
            path += "\\custom\\" + file_name;

            var sr = File.OpenText(path);
            this.myedit.Value = sr.ReadToEnd();
            sr.Close();
            sr.Dispose();
        }

        public string get_file_name(string which_file)
        {
            var file_name = "";

            if (which_file == "css")
                file_name = "btnet_custom.css";
            else if (which_file == "footer")
                file_name = "custom_footer.html";
            else if (which_file == "header")
                file_name = "custom_header.html";
            else if (which_file == "logo")
                file_name = "custom_logo.html";
            else if (which_file == "welcome") file_name = "custom_welcome.html";

            return file_name;
        }
    }
}
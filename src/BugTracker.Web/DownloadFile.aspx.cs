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

    public partial class DownloadFile : Page
    {
        public Security Security;

        public void Page_Load(object sender, EventArgs e)
        {
            this.Security = new Security();
            this.Security.CheckSecurity(HttpContext.Current, Security.MustBeAdmin);

            var which = Request["which"];
            var filename = Request["filename"];

            if (string.IsNullOrEmpty(filename) || string.IsNullOrEmpty(which)) Response.End();

            var path = "";

            if (which == "backup")
                path = HttpContext.Current.Server.MapPath(null) + "\\App_Data\\" + filename;
            else if (which == "log")
                path = HttpContext.Current.Server.MapPath(null) + "\\App_Data\\logs\\" + filename;
            else
                Response.End();

            Response.ContentType = Util.FilenameToContentType(filename);
            Response.AddHeader("content-disposition", "attachment; filename=\"" + filename + "\"");

            if (Util.GetSetting("UseTransmitFileInsteadOfWriteFile", "0") == "1")
                Response.TransmitFile(path);
            else
                Response.WriteFile(path);
        }
    }
}
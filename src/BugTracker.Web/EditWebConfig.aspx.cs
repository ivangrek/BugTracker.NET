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
    using System.Xml;
    using Core;

    public partial class EditWebConfig : Page
    {
        public Security Security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            this.Security = new Security();
            this.Security.CheckSecurity(HttpContext.Current, Security.MustBeAdmin);

            Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "edit Web.config";

            var path = HttpContext.Current.Server.MapPath(null);
            path += "\\Web.config";

            if (!IsPostBack)
            {
                var sr = File.OpenText(path);
                this.myedit.Value = sr.ReadToEnd();
                sr.Close();
                sr.Dispose();
                this.msg.InnerHtml = "&nbsp;";
            }
            else
            {
                var doc = new XmlDocument();
                var stringReader = new StringReader(this.myedit.Value);
                try
                {
                    doc.Load(stringReader);
                    var sw = File.CreateText(path);
                    sw.Write(this.myedit.Value);
                    sw.Close();
                    sw.Dispose();
                    this.msg.InnerHtml = "Web.config was saved.";
                }
                catch (Exception ex)
                {
                    this.msg.InnerHtml = "ERROR:" + ex.Message;
                }
            }
        }
    }
}
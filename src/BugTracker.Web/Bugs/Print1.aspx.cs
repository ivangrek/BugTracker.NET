/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Bugs
{
    using System;
    using System.Data;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class Print1 : Page
    {
        public DataRow Dr;
        public bool HistoryInline;
        public bool ImagesInline;
        public Security Security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            this.Security = new Security();
            this.Security.CheckSecurity(HttpContext.Current, Security.AnyUserOk);

            var stringBugid = Request.QueryString["id"];

            var bugid = Convert.ToInt32(stringBugid);

            this.Dr = Bug.GetBugDataRow(bugid, this.Security);

            if (this.Dr == null)
            {
                Util.DisplayBugNotFound(Response, this.Security, bugid);
                return;
            }

            Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - "
                                                                        + Util.CapitalizeFirstLetter(
                                                                            Util.GetSetting("SingularBugLabel",
                                                                                "bug"))
                                                                        + " ID" + stringBugid + " " +
                                                                        (string) this.Dr["short_desc"];

            // don't allow user to view a bug he is not allowed to view
            if ((int) this.Dr["pu_permission_level"] == 0)
            {
                Util.DisplayYouDontHavePermission(Response, this.Security);
                return;
            }

            var cookie = Request.Cookies["images_inline"];
            if (cookie == null || cookie.Value == "0")
                this.ImagesInline = false;
            else
                this.ImagesInline = true;

            cookie = Request.Cookies["history_inline"];
            if (cookie == null || cookie.Value == "0")
                this.HistoryInline = false;
            else
                this.HistoryInline = true;
        }
    }
}
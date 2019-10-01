/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Bugs
{
    using System;
    using System.Data;
    using System.Web.UI;
    using Core;

    public partial class Print1 : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public ISecurity Security { get; set; }

        public DataRow Dr;
        public bool HistoryInline;
        public bool ImagesInline;
        public int Id;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.AnyUserOk);

            var stringBugid = Request.QueryString["id"];
            var bugid = Convert.ToInt32(stringBugid);

            Id = bugid;
            this.Dr = Bug.GetBugDataRow(bugid, Security);

            if (this.Dr == null)
            {
                this.mainBlock.Visible = false;
                this.errorBlock.Visible = true;
                this.errorBlockPermissions.Visible = false;

                MainMenu.SelectedItem = ApplicationSettings.PluralBugLabel;

                return;
            }

            Page.Title = $"{ApplicationSettings.AppTitle} - "
                                                                        + Util.CapitalizeFirstLetter(
                                                                            ApplicationSettings.SingularBugLabel)
                                                                        + " ID" + stringBugid + " " +
                                                                        (string) this.Dr["short_desc"];

            // don't allow user to view a bug he is not allowed to view
            if ((int) this.Dr["pu_permission_level"] == 0)
            {
                this.mainBlock.Visible = false;
                this.errorBlock.Visible = false;
                this.errorBlockPermissions.Visible = true;

                MainMenu.SelectedItem = ApplicationSettings.PluralBugLabel;

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
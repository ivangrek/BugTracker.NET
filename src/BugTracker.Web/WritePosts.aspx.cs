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

    public partial class WritePosts : Page
    {
        public Security Security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            this.Security = new Security();
            this.Security.CheckSecurity(HttpContext.Current, Security.AnyUserOk);

            var bugid = Convert.ToInt32(Request["id"]);
            var imagesInline = Request["images_inline"] == "1";
            var historyInline = Request["history_inline"] == "1";

            var permissionLevel = Bug.GetBugPermissionLevel(bugid, this.Security);
            if (permissionLevel == Security.PermissionNone)
            {
                Response.Write("You are not allowed to view this item");
                Response.End();
            }

            var dsPosts = Core.PrintBug.GetBugPosts(bugid, this.Security.User.ExternalUser, historyInline);

            Core.PrintBug.WritePosts(
                dsPosts,
                Response,
                bugid,
                permissionLevel,
                true, // write links
                imagesInline,
                historyInline,
                true, // internal_posts
                this.Security.User);
        }
    }
}
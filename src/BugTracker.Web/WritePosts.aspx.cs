/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Web.UI;
    using Core;

    public partial class WritePosts : Page
    {
        public ISecurity Security { get; set; }

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.AnyUserOk);

            var bugid = Convert.ToInt32(Request["id"]);
            var imagesInline = Request["images_inline"] == "1";
            var historyInline = Request["history_inline"] == "1";

            var permissionLevel = Bug.GetBugPermissionLevel(bugid, Security);
            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                Response.Write("You are not allowed to view this item");
                Response.End();
            }

            var dsPosts = PrintBug.GetBugPosts(bugid, Security.User.ExternalUser, historyInline);
            var (_, html) = PrintBug.WritePosts(
                dsPosts,
                bugid,
                permissionLevel,
                true, // write links
                imagesInline,
                historyInline,
                true, // internal_posts
                Security.User);

            Response.Write(html);
        }
    }
}
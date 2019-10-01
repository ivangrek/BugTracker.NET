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

    public partial class Ajax : Page
    {
        public ISecurity Security { get; set; }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.AnyUserOk);

            var bugid = Util.SanitizeInteger(Request["bugid"]);

            // check permission
            var permissionLevel = Bug.GetBugPermissionLevel(Convert.ToInt32(bugid), Security);
            if (permissionLevel != SecurityPermissionLevel.PermissionNone)
            {
                Response.Write(@"

<style>
.cmt_text
{
font-family: courier new;
font-size: 8pt;
}
.pst
{
font-size: 7pt;
}
</style>");

                var intBugid = Convert.ToInt32(bugid);
                var dsPosts = PrintBug.GetBugPosts(intBugid, Security.User.ExternalUser, false);
                var postCnt = PrintBug.WritePosts(
                    dsPosts,
                    Response,
                    intBugid,
                    permissionLevel,
                    false, // write links
                    false, // images inline
                    false, // history inline
                    true, // internal posts
                    Security.User);

                // We can't unwrite what we wrote, but let's tell javascript to ignore it.
                if (postCnt == 0) Response.Write("<!--zeroposts-->");
            }
            else
            {
                Response.Write("");
            }
        }
    }
}
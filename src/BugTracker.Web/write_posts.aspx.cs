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

    public partial class write_posts : Page
    {
        public Security security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            var bugid = Convert.ToInt32(Request["id"]);
            var images_inline = Request["images_inline"] == "1";
            var history_inline = Request["history_inline"] == "1";

            var permission_level = Bug.get_bug_permission_level(bugid, this.security);
            if (permission_level == Security.PERMISSION_NONE)
            {
                Response.Write("You are not allowed to view this item");
                Response.End();
            }

            var ds_posts = PrintBug.get_bug_posts(bugid, this.security.user.external_user, history_inline);

            PrintBug.write_posts(
                ds_posts,
                Response,
                bugid,
                permission_level,
                true, // write links
                images_inline,
                history_inline,
                true, // internal_posts
                this.security.user);
        }
    }
}
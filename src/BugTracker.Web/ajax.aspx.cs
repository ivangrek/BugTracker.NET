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

    public partial class ajax : Page
    {
        public Security security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            var bugid = Util.sanitize_integer(Request["bugid"]);

            // check permission
            var permission_level = Bug.get_bug_permission_level(Convert.ToInt32(bugid), this.security);
            if (permission_level != Security.PERMISSION_NONE)
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

                var int_bugid = Convert.ToInt32(bugid);
                var ds_posts = PrintBug.get_bug_posts(int_bugid, this.security.user.external_user, false);
                var post_cnt = PrintBug.write_posts(
                    ds_posts,
                    Response,
                    int_bugid,
                    permission_level,
                    false, // write links
                    false, // images inline
                    false, // history inline
                    true, // internal posts
                    this.security.user);

                // We can't unwrite what we wrote, but let's tell javascript to ignore it.
                if (post_cnt == 0) Response.Write("<!--zeroposts-->");
            }
            else
            {
                Response.Write("");
            }
        }
    }
}
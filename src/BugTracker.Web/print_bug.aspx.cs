/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Data;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class print_bug : Page
    {
        public DataRow dr;
        public bool history_inline;
        public bool images_inline;
        public Security security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            var string_bugid = Request.QueryString["id"];

            var bugid = Convert.ToInt32(string_bugid);

            this.dr = Bug.get_bug_datarow(bugid, this.security);

            if (this.dr == null)
            {
                Util.display_bug_not_found(Response, this.security, bugid);
                return;
            }

            Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                        + Util.capitalize_first_letter(
                                                                            Util.get_setting("SingularBugLabel",
                                                                                "bug"))
                                                                        + " ID" + string_bugid + " " +
                                                                        (string) this.dr["short_desc"];

            // don't allow user to view a bug he is not allowed to view
            if ((int) this.dr["pu_permission_level"] == 0)
            {
                Util.display_you_dont_have_permission(Response, this.security);
                return;
            }

            var cookie = Request.Cookies["images_inline"];
            if (cookie == null || cookie.Value == "0")
                this.images_inline = false;
            else
                this.images_inline = true;

            cookie = Request.Cookies["history_inline"];
            if (cookie == null || cookie.Value == "0")
                this.history_inline = false;
            else
                this.history_inline = true;
        }
    }
}
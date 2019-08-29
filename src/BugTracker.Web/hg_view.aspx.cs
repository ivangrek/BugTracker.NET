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

    public partial class hg_view : Page
    {
        public Security security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);
            Response.ContentType = "text/plain";

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            var sql = @"
select hgrev_revision, hgrev_bug, hgrev_repository, hgap_path 
from hg_revisions
inner join hg_affected_paths on hgap_hgrev_id = hgrev_id
where hgap_id = $id";

            var hgap_id = Convert.ToInt32(Util.sanitize_integer(Request["revpathid"]));
            sql = sql.Replace("$id", Convert.ToString(hgap_id));

            var dr = DbUtil.get_datarow(sql);

            // check if user has permission for this bug
            var permission_level = Bug.get_bug_permission_level((int) dr["hgrev_bug"], this.security);
            if (permission_level == Security.PERMISSION_NONE)
            {
                Response.Write("You are not allowed to view this item");
                Response.End();
            }

            var repo = (string) dr["hgrev_repository"];
            var path = (string) dr["hgap_path"];
            var revision = Request["rev"];

            var text = VersionControl.hg_get_file_contents(repo, revision, path);

            Response.Write(text);
        }
    }
}
/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Versioning.Hg
{
    using System;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class Blame : Page
    {
        public string BlameText;
        public string Path;
        public string Revision;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            var security = new Security();

            security.CheckSecurity(Security.AnyUserOk);

            Page.Title = "hg blame " + HttpUtility.HtmlEncode(this.Revision) + " -- " + HttpUtility.HtmlEncode(this.Path);

            var sql = @"
select hgrev_revision, hgrev_bug, hgrev_repository, hgap_path 
from hg_revisions
inner join hg_affected_paths on hgap_hgrev_id = hgrev_id
where hgap_id = $id";

            var hgapId = Convert.ToInt32(Util.SanitizeInteger(Request["revpathid"]));
            sql = sql.Replace("$id", Convert.ToString(hgapId));

            var dr = DbUtil.GetDataRow(sql);

            // check if user has permission for this bug
            var permissionLevel = Bug.GetBugPermissionLevel((int) dr["hgrev_bug"], security);
            if (permissionLevel == Security.PermissionNone)
            {
                Response.Write("You are not allowed to view this item");
                Response.End();
            }

            var repo = (string) dr["hgrev_repository"];
            this.Path = (string) dr["hgap_path"];
            this.Revision = Request["rev"];

            this.BlameText = VersionControl.HgBlame(repo, this.Path, this.Revision);
        }

        public void write_blame(string blameText)
        {
            Response.Write(HttpUtility.HtmlEncode(blameText));
        }
    }
}
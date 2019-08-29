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
    using System.Xml;
    using Core;

    public partial class hg_diff : Page
    {
        public string left_out = "";
        public string left_title = "";
        public string path = "";
        public string right_out = "";
        public string right_title = "";
        public Security security;
        public string unified_diff_text = "";

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            Page.Title = "hg diff " + HttpUtility.HtmlEncode(this.path);

            // get info about revision

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
            this.path = (string) dr["hgap_path"];

            var error = "";

            var revision0 = Request["rev_0"];

            if (string.IsNullOrEmpty(revision0))
            {
                var revision = Convert.ToString((int) dr["hgrev_revision"]);

                // we need to find the previous revision
                var log = VersionControl.hg_log(repo, revision, this.path);
                var prev_revision = get_previous_revision(log, revision);

                if (prev_revision == "")
                {
                    Response.Write("unable to determine previous revision from log");
                    Response.End();
                }

                this.unified_diff_text =
                    VersionControl.hg_get_unified_diff_two_revisions(repo, prev_revision, revision, this.path);

                // get the source code for both the left and right
                var left_text = VersionControl.hg_get_file_contents(repo, prev_revision, this.path);

                var right_text = VersionControl.hg_get_file_contents(repo, revision, this.path);
                this.left_title = prev_revision;
                this.right_title = revision;

                error = VersionControl.visual_diff(this.unified_diff_text, left_text, right_text, ref this.left_out,
                    ref this.right_out);
            }
            else
            {
                var revision1 = Request["rev_1"];

                this.unified_diff_text =
                    VersionControl.hg_get_unified_diff_two_revisions(repo, revision0, revision1, this.path);

                // get the source code for both the left and right
                var left_text = VersionControl.hg_get_file_contents(repo, revision0, this.path);
                var right_text = VersionControl.hg_get_file_contents(repo, revision1, this.path);
                this.left_title = revision0;
                this.right_title = revision1;

                error = VersionControl.visual_diff(this.unified_diff_text, left_text, right_text, ref this.left_out,
                    ref this.right_out);
            }

            if (error != "")
            {
                Response.Write(HttpUtility.HtmlEncode(error));
                Response.End();
            }
        }

        public string get_previous_revision(string log_result, string this_revision)
        {
            var doc = new XmlDocument();
            doc.LoadXml("<log>" + log_result + "</log>");
            var revisions = doc.GetElementsByTagName("changeset");

            // read backwards
            if (revisions.Count > 1)
            {
                var changeset = (XmlElement) revisions[revisions.Count - 2];

                return changeset.GetAttribute("rev");
            }

            return "";
        }
    }
}
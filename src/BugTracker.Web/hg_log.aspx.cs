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

    public partial class hg_log : Page
    {
        public string file_path;

        public string log_result;
        public string repo;
        public Security security;
        public string string_affected_path_id;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            Page.Title = "hg log " + HttpUtility.HtmlEncode(this.file_path);

            // get info about revision

            var sql = @"
select hgrev_repository, hgrev_revision, hgap_path, hgrev_bug
from hg_revisions
inner join hg_affected_paths on hgap_hgrev_id = hgrev_id
where hgap_id = $id
order by hgrev_revision desc, hgap_path";

            var hgap_id = Convert.ToInt32(Util.sanitize_integer(Request["revpathid"]));

            this.string_affected_path_id = Convert.ToString(hgap_id);
            sql = sql.Replace("$id", this.string_affected_path_id);

            var dr = DbUtil.get_datarow(sql);

            // check if user has permission for this bug
            var bugid = (int) dr["hgrev_bug"];

            var permission_level = Bug.get_bug_permission_level(bugid, this.security);
            if (permission_level == Security.PERMISSION_NONE)
            {
                Response.Write("You are not allowed to view this item");
                Response.End();
            }

            this.revpathid.Value = this.string_affected_path_id;

            this.repo = (string) dr["hgrev_repository"];
            var revision = Convert.ToString((int) dr["hgrev_revision"]);
            this.file_path = (string) dr["hgap_path"];

            this.log_result = VersionControl.hg_log(this.repo, revision, this.file_path);
        }

        public void fetch_and_write_history()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<log>" + this.log_result + "</log>");

            var revisions = doc.GetElementsByTagName("changeset");

            var row = 0;

            // read backwards
            for (var i = revisions.Count - 1; i > -1; i--)
            {
                var changeset = (XmlElement) revisions[i];

                var revision = changeset.GetAttribute("node");
                var author = changeset.GetElementsByTagName("auth")[0].InnerText;
                var date = changeset.GetElementsByTagName("date")[0].InnerText;
                var desc = changeset.GetElementsByTagName("desc")[0].InnerText;
                var path = changeset.GetElementsByTagName("file")[0].InnerText;

                Response.Write("<tr><td class=datad>" + revision);
                Response.Write("<td class=datad>" + author);
                Response.Write("<td class=datad>" + date);
                Response.Write("<td class=datad>" + path);
                //        Response.Write("<td class=datad>" + action);
                //        Response.Write("<td class=datad>" + copy_from);
                //        Response.Write("<td class=datad>" + copy_from_rev);

                Response.Write("<td class=datad>" + desc.Replace(Environment.NewLine, "<br/>"));

                Response.Write(
                    "<td class=datad><a target=_blank href=hg_view.aspx?revpathid=" + this.string_affected_path_id
                                                                                    + "&rev=" + revision
                                                                                    + ">");

                Response.Write("view</a>");

                Response.Write(
                    "<td class=datad><a target=_blank href=hg_blame.aspx?revpathid=" + this.string_affected_path_id
                                                                                     + "&rev=" + revision
                                                                                     + ">");

                Response.Write("annotated</a>");

                Response.Write("<td class=datad><a id=" + revision
                                                        + " href='javascript:sel_for_diff("
                                                        + Convert.ToString(++row)
                                                        + ",\""
                                                        + revision
                                                        + "\",\"\")'>select for diff</a>");
            }
        }
    }
}
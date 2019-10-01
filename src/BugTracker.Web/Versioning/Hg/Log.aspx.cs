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
    using System.Xml;
    using Core;

    public partial class Log : Page
    {
        public ISecurity Security { get; set; }

        public string FilePath;

        public string LogResult;
        public string Repo;
        public string StringAffectedPathId;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.AnyUserOk);

            Page.Title = "hg log " + HttpUtility.HtmlEncode(this.FilePath);

            // get info about revision

            var sql = @"
select hgrev_repository, hgrev_revision, hgap_path, hgrev_bug
from hg_revisions
inner join hg_affected_paths on hgap_hgrev_id = hgrev_id
where hgap_id = $id
order by hgrev_revision desc, hgap_path";

            var hgapId = Convert.ToInt32(Util.SanitizeInteger(Request["revpathid"]));

            this.StringAffectedPathId = Convert.ToString(hgapId);
            sql = sql.Replace("$id", this.StringAffectedPathId);

            var dr = DbUtil.GetDataRow(sql);

            // check if user has permission for this bug
            var bugid = (int) dr["hgrev_bug"];

            var permissionLevel = Bug.GetBugPermissionLevel(bugid, Security);
            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                Response.Write("You are not allowed to view this item");
                Response.End();
            }

            this.revpathid.Value = this.StringAffectedPathId;

            this.Repo = (string) dr["hgrev_repository"];
            var revision = Convert.ToString((int) dr["hgrev_revision"]);
            this.FilePath = (string) dr["hgap_path"];

            this.LogResult = VersionControl.HgLog(this.Repo, revision, this.FilePath);
        }

        public void fetch_and_write_history()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<log>" + this.LogResult + "</log>");

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
                    "<td class=datad><a target=_blank href=" + ResolveUrl("~/Versioning/Hg/View.aspx") + @"?revpathid=" + this.StringAffectedPathId
                                                                                    + "&rev=" + revision
                                                                                    + ">");

                Response.Write("view</a>");

                Response.Write(
                    "<td class=datad><a target=_blank href=" + ResolveUrl("~/Versioning/Hg/Blame.aspx") + @"?revpathid=" + this.StringAffectedPathId
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
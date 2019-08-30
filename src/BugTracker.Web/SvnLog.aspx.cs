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

    public partial class SvnLog : Page
    {
        public string FilePath;
        public string Log;
        public string Repo;
        public int Rev;
        public Security Security;

        public string StringAffectedPathId;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            this.Security = new Security();
            this.Security.CheckSecurity(HttpContext.Current, Security.AnyUserOk);

            Page.Title = "svn log" + HttpUtility.HtmlEncode(this.FilePath);

            // get info about revision

            var sql = @"
select svnrev_revision, svnrev_repository, svnap_path, svnrev_bug
from svn_revisions
inner join svn_affected_paths on svnap_svnrev_id = svnrev_id
where svnap_id = $id
order by svnrev_revision desc, svnap_path";

            var svnapId = Convert.ToInt32(Util.SanitizeInteger(Request["revpathid"]));
            this.StringAffectedPathId = Convert.ToString(svnapId);

            sql = sql.Replace("$id", this.StringAffectedPathId);

            var dr = DbUtil.GetDataRow(sql);

            // check if user has permission for this bug
            var permissionLevel = Bug.GetBugPermissionLevel((int) dr["svnrev_bug"], this.Security);
            if (permissionLevel == Security.PermissionNone)
            {
                Response.Write("You are not allowed to view this item");
                Response.End();
            }

            this.revpathid.Value = this.StringAffectedPathId;

            this.Repo = (string) dr["svnrev_repository"];
            this.FilePath = (string) dr["svnap_path"];
            this.Rev = (int) dr["svnrev_revision"];

            this.Log = VersionControl.SvnLog(this.Repo, this.FilePath, this.Rev);

            if (this.Log.StartsWith("ERROR:"))
            {
                Response.Write(HttpUtility.HtmlEncode(this.Log));
                Response.End();
            }
        }

        public void fetch_and_write_history(string filePath)
        {
            var doc = new XmlDocument();
            doc.LoadXml(this.Log);
            var logNode = doc.ChildNodes[1];
            //string adjusted_file_path = "/" + file_path; // when/why did this stop working?
            var adjustedFilePath = filePath;

            var row = 0;
            foreach (XmlElement logentry in logNode)
            {
                var revision = logentry.GetAttribute("revision");
                var author = "";
                var date = "";
                var path = "";
                var action = "";
                //string copy_from = "";
                //string copy_from_rev = "";
                var msg = "";

                foreach (XmlNode node in logentry.ChildNodes)
                    if (node.Name == "author") author = node.InnerText;
                    else if (node.Name == "date")
                        date = Util.FormatDbDateTime(XmlConvert.ToDateTime(node.InnerText,
                            XmlDateTimeSerializationMode.Local));
                    else if (node.Name == "msg") msg = node.InnerText;
                    else if (node.Name == "paths")
                        foreach (XmlNode pathNode in node.ChildNodes)
                            if (pathNode.InnerText == adjustedFilePath)
                            {
                                var pathEl = (XmlElement) pathNode;
                                action = pathEl.GetAttribute("action");
                                if (!action.Contains("D"))
                                {
                                    path = pathNode.InnerText;
                                    path = adjustedFilePath;
                                    if (pathEl.GetAttribute("copyfrom-path") != "")
                                        adjustedFilePath = pathEl.GetAttribute("copyfrom-path");
                                }
                            }

                Response.Write("<tr><td class=datad>" + revision);
                Response.Write("<td class=datad>" + author);
                Response.Write("<td class=datad>" + date);
                Response.Write("<td class=datad>" + path);
                Response.Write("<td class=datad>" + action);
                //        Response.Write("<td class=datad>" + copy_from);
                //        Response.Write("<td class=datad>" + copy_from_rev);
                Response.Write("<td class=datad>" + msg.Replace(Environment.NewLine, "<br/>"));

                Response.Write(
                    "<td class=datad><a target=_blank href=SvnView.aspx?revpathid=" + this.StringAffectedPathId
                                                                                     + "&rev=" + revision
                                                                                     + "&path=" +
                                                                                     HttpUtility.UrlEncode(path)
                                                                                     + ">");

                Response.Write("view</a>");

                Response.Write(
                    "<td class=datad><a target=_blank href=SvnBlame.aspx?revpathid=" + this.StringAffectedPathId
                                                                                      + "&rev=" + revision
                                                                                      + "&path=" +
                                                                                      HttpUtility.UrlEncode(path)
                                                                                      + ">");

                Response.Write("annotated</a>");

                Response.Write("<td class=datad><a id=" + revision
                                                        + " href='javascript:sel_for_diff("
                                                        + Convert.ToString(row)
                                                        + ",\""
                                                        + revision
                                                        + "\",\""
                                                        + path
                                                        + "\")'>select for diff</a>");
            }
        }
    }
}
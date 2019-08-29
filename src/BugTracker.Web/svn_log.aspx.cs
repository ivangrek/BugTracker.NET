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

    public partial class svn_log : Page
    {
        public string file_path;
        public string log;
        public string repo;
        public int rev;
        public Security security;

        public string string_affected_path_id;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            Page.Title = "svn log" + HttpUtility.HtmlEncode(this.file_path);

            // get info about revision

            var sql = @"
select svnrev_revision, svnrev_repository, svnap_path, svnrev_bug
from svn_revisions
inner join svn_affected_paths on svnap_svnrev_id = svnrev_id
where svnap_id = $id
order by svnrev_revision desc, svnap_path";

            var svnap_id = Convert.ToInt32(Util.sanitize_integer(Request["revpathid"]));
            this.string_affected_path_id = Convert.ToString(svnap_id);

            sql = sql.Replace("$id", this.string_affected_path_id);

            var dr = DbUtil.get_datarow(sql);

            // check if user has permission for this bug
            var permission_level = Bug.get_bug_permission_level((int) dr["svnrev_bug"], this.security);
            if (permission_level == Security.PERMISSION_NONE)
            {
                Response.Write("You are not allowed to view this item");
                Response.End();
            }

            this.revpathid.Value = this.string_affected_path_id;

            this.repo = (string) dr["svnrev_repository"];
            this.file_path = (string) dr["svnap_path"];
            this.rev = (int) dr["svnrev_revision"];

            this.log = VersionControl.svn_log(this.repo, this.file_path, this.rev);

            if (this.log.StartsWith("ERROR:"))
            {
                Response.Write(HttpUtility.HtmlEncode(this.log));
                Response.End();
            }
        }

        public void fetch_and_write_history(string file_path)
        {
            var doc = new XmlDocument();
            doc.LoadXml(this.log);
            var log_node = doc.ChildNodes[1];
            //string adjusted_file_path = "/" + file_path; // when/why did this stop working?
            var adjusted_file_path = file_path;

            var row = 0;
            foreach (XmlElement logentry in log_node)
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
                        date = Util.format_db_date_and_time(XmlConvert.ToDateTime(node.InnerText,
                            XmlDateTimeSerializationMode.Local));
                    else if (node.Name == "msg") msg = node.InnerText;
                    else if (node.Name == "paths")
                        foreach (XmlNode path_node in node.ChildNodes)
                            if (path_node.InnerText == adjusted_file_path)
                            {
                                var path_el = (XmlElement) path_node;
                                action = path_el.GetAttribute("action");
                                if (!action.Contains("D"))
                                {
                                    path = path_node.InnerText;
                                    path = adjusted_file_path;
                                    if (path_el.GetAttribute("copyfrom-path") != "")
                                        adjusted_file_path = path_el.GetAttribute("copyfrom-path");
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
                    "<td class=datad><a target=_blank href=svn_view.aspx?revpathid=" + this.string_affected_path_id
                                                                                     + "&rev=" + revision
                                                                                     + "&path=" +
                                                                                     HttpUtility.UrlEncode(path)
                                                                                     + ">");

                Response.Write("view</a>");

                Response.Write(
                    "<td class=datad><a target=_blank href=svn_blame.aspx?revpathid=" + this.string_affected_path_id
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
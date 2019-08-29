/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Text.RegularExpressions;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class git_log : Page
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

            Page.Title = "git log " + HttpUtility.HtmlEncode(this.file_path);

            // get info about commit

            var sql = @"
select gitcom_repository, gitcom_commit, gitap_path, gitcom_bug
from git_commits
inner join git_affected_paths on gitap_gitcom_id = gitcom_id
where gitap_id = $id
order by gitcom_commit desc, gitap_path";

            var gitap_id = Convert.ToInt32(Util.sanitize_integer(Request["revpathid"]));

            this.string_affected_path_id = Convert.ToString(gitap_id);
            sql = sql.Replace("$id", this.string_affected_path_id);

            var dr = DbUtil.get_datarow(sql);

            // check if user has permission for this bug
            var bugid = (int) dr["gitcom_bug"];

            var permission_level = Bug.get_bug_permission_level(bugid, this.security);
            if (permission_level == Security.PERMISSION_NONE)
            {
                Response.Write("You are not allowed to view this item");
                Response.End();
            }

            this.revpathid.Value = this.string_affected_path_id;

            this.repo = (string) dr["gitcom_repository"];
            var commit = (string) dr["gitcom_commit"];
            this.file_path = (string) dr["gitap_path"];

            this.log_result = VersionControl.git_log(this.repo, commit, this.file_path);
        }

        public void write_line(int row, string commit, string author, string date, string path, string action,
            string msg)
        {
            Response.Write("<tr><td class=datad>" + commit);
            Response.Write("<td class=datad>" + author);
            Response.Write("<td class=datad>" + date);
            Response.Write("<td class=datad>" + path);
            Response.Write("<td class=datad>" + action);
            Response.Write("<td class=datad>" + msg.Replace(Environment.NewLine, "<br/>"));

            Response.Write(
                "<td class=datad><a target=_blank href=git_view.aspx?revpathid=" + this.string_affected_path_id
                                                                                 + "&commit=" + commit
                                                                                 + ">");

            Response.Write("view</a>");

            Response.Write(
                "<td class=datad><a target=_blank href=git_blame.aspx?revpathid=" + this.string_affected_path_id
                                                                                  + "&commit=" + commit
                                                                                  + ">");

            Response.Write("annotated</a>");

            Response.Write("<td class=datad><a id=" + commit
                                                    + " href='javascript:sel_for_diff("
                                                    + Convert.ToString(row)
                                                    + ",\""
                                                    + commit
                                                    + "\",\"\")'>select for diff</a>");
        }

        public void fetch_and_write_history()
        {
            /*
        commit 789e948bce733dab9605bf8eb51584e3b9a2eba3
        Author: corey <ctrager@yahoo.com>
        Date:   2009-10-11 21:54:14 -0500

            123 just 8 lines

        M	dir1/file3.txt

        commit 0b77adbedfab04185a3c1d33afe25aa330e91518
        Author: corey <ctrager@yahoo.com>
        Date:   2009-10-11 21:24:12 -0500

            123 just 8 lines

        M	dir1/file3.txt

        */
            var regex = new Regex("\n");
            var lines = regex.Split(this.log_result);

            var commit = "";
            var author = "";
            var date = "";
            var path = "";
            var action = "";
            var msg = "";
            var row = 0;

            for (var i = 0; i < lines.Length; i++)
                if (lines[i].StartsWith("commit "))
                {
                    if (commit != "")
                    {
                        write_line(++row, commit, author, date, path, action, msg);
                        commit = "";
                        author = "";
                        date = "";
                        path = "";
                        action = "";
                        msg = "";
                    }

                    commit = lines[i].Substring(7);
                }
                else if (lines[i].StartsWith("Author: "))
                {
                    author = Server.HtmlEncode(lines[i].Substring(8));
                }
                else if (lines[i].StartsWith("Date: "))
                {
                    date = lines[i].Substring(8, 19);
                }
                else if (lines[i].StartsWith("    "))
                {
                    if (msg != "") msg += Environment.NewLine;
                    msg += lines[i].Substring(4);
                }
                else if (lines[i].Length > 1 && lines[i][1] == '\t')
                {
                    action = lines[i].Substring(0, 1);
                    path = lines[i].Substring(2);
                }

            if (commit != "") write_line(++row, commit, author, date, path, action, msg);
        }
    }
}
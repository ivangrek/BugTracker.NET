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

    public partial class git_blame : Page
    {
        public string blame_text;
        public string commit;
        public string path;
        public Security security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            Page.Title = "git blame " + this.commit + " -- " + HttpUtility.HtmlEncode(this.path);

            var sql = @"
select gitcom_commit, gitcom_bug, gitcom_repository, gitap_path 
from git_commits
inner join git_affected_paths on gitap_gitcom_id = gitcom_id
where gitap_id = $id";

            var gitap_id = Convert.ToInt32(Util.sanitize_integer(Request["revpathid"]));
            sql = sql.Replace("$id", Convert.ToString(gitap_id));

            var dr = DbUtil.get_datarow(sql);

            // check if user has permission for this bug
            var permission_level = Bug.get_bug_permission_level((int) dr["gitcom_bug"], this.security);
            if (permission_level == Security.PERMISSION_NONE)
            {
                Response.Write("You are not allowed to view this item");
                Response.End();
            }

            var repo = (string) dr["gitcom_repository"];
            this.path = (string) dr["gitap_path"];
            this.commit = Request["commit"];

            this.blame_text = VersionControl.git_blame(repo, this.path, this.commit);
        }

        public void write_blame(string blame_text)
        {
            /*
        f36d6c45 (corey 2009-10-04 19:44:42 -0500  1) asdfasdf
        f36d6c45 (corey 2009-10-04 19:44:42 -0500  2) asdf
        9f3ac5e7 (corey 2009-10-04 19:46:05 -0500  3) asdfab
        */

            var regex = new Regex("\n");
            var lines = regex.Split(blame_text);

            for (var i = 0; i < lines.Length; i++)
                if (lines[i].Length > 40)
                {
                    string commit;
                    string author;
                    string text;
                    string date;

                    commit = lines[i].Substring(0, 8);
                    var pos = lines[i].IndexOf(" ", 11); // position of space after author
                    author = lines[i].Substring(10, pos - 10);
                    date = lines[i].Substring(pos + 1, 19);
                    pos = lines[i].IndexOf(")", 40);
                    text = lines[i].Substring(pos + 2);

                    Response.Write("<tr><td>");
                    Response.Write(commit);
                    Response.Write("&nbsp;<td nowrap>" + author);
                    Response.Write("<td nowrap style='background: #ddffdd'><pre style='display:inline;'> " +
                                   HttpUtility.HtmlEncode(text));
                    Response.Write(" </pre><td nowrap>" + date);
                }
        }
    }
}
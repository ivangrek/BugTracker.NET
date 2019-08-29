/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Collections;
    using System.Data;
    using System.IO;
    using System.Text;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class edit_styles : Page
    {
        public DataSet ds;

        public Security security;

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.MUST_BE_ADMIN);

            this.ds = DbUtil.get_dataset(
                @"select
			'<a target=_blank href=edit_priority.aspx?id=' + convert(varchar,pr_id) + '>' + pr_name + '</a>' [priority],
			'<a target=_blank href=edit_status.aspx?id=' + convert(varchar,st_id) + '>' + st_name + '</a>' [status],
			isnull(pr_style,'') [priority CSS class],
			isnull(st_style,'') [status CSS class],
			isnull(pr_style + st_style,'datad') [combo CSS class - priority + status ],
			'<span class=''' + isnull(pr_style,'') + isnull(st_style,'')  +'''>The quick brown fox</span>' [text sample]
			from priorities, statuses /* intentioanl cartesian join */
			order by pr_sort_seq, st_sort_seq;

			select distinct isnull(pr_style + st_style,'datad')
			from priorities, statuses;");

            var classes_list = new ArrayList();
            foreach (DataRow dr_styles in this.ds.Tables[1].Rows) classes_list.Add("." + (string) dr_styles[0]);

            // create path
            var map_path = (string) HttpRuntime.Cache["MapPath"];
            var path = map_path + "\\custom\\btnet_custom.css";

            var relevant_css_lines = new StringBuilder();

            var lines = new ArrayList();
            if (File.Exists(path))
            {
                string line;
                var stream = File.OpenText(path);
                while ((line = stream.ReadLine()) != null)
                    for (var i = 0; i < classes_list.Count; i++)
                        if (line.IndexOf((string) classes_list[i]) > -1)
                        {
                            relevant_css_lines.Append(line);
                            relevant_css_lines.Append("<br>");
                            lines.Add(line);
                            break;
                        }

                stream.Close();
            }

            this.relevant_lines.InnerHtml = relevant_css_lines.ToString();
        }
    }
}
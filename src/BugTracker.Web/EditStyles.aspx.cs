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

    public partial class EditStyles : Page
    {
        public DataSet Ds;

        public Security Security;

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            this.Security = new Security();
            this.Security.CheckSecurity(HttpContext.Current, Security.MustBeAdmin);

            this.Ds = DbUtil.GetDataSet(
                @"select
			'<a target=_blank href=EditPriority.aspx?id=' + convert(varchar,pr_id) + '>' + pr_name + '</a>' [priority],
			'<a target=_blank href=EditStatus.aspx?id=' + convert(varchar,st_id) + '>' + st_name + '</a>' [status],
			isnull(pr_style,'') [priority CSS class],
			isnull(st_style,'') [status CSS class],
			isnull(pr_style + st_style,'datad') [combo CSS class - priority + status ],
			'<span class=''' + isnull(pr_style,'') + isnull(st_style,'')  +'''>The quick brown fox</span>' [text sample]
			from priorities, statuses /* intentioanl cartesian join */
			order by pr_sort_seq, st_sort_seq;

			select distinct isnull(pr_style + st_style,'datad')
			from priorities, statuses;");

            var classesList = new ArrayList();
            foreach (DataRow drStyles in this.Ds.Tables[1].Rows) classesList.Add("." + (string) drStyles[0]);

            // create path
            var mapPath = (string) HttpRuntime.Cache["MapPath"];
            var path = mapPath + "\\Content\\custom\\btnet_custom.css";

            var relevantCssLines = new StringBuilder();

            var lines = new ArrayList();
            if (File.Exists(path))
            {
                string line;
                var stream = File.OpenText(path);
                while ((line = stream.ReadLine()) != null)
                    for (var i = 0; i < classesList.Count; i++)
                        if (line.IndexOf((string) classesList[i]) > -1)
                        {
                            relevantCssLines.Append(line);
                            relevantCssLines.Append("<br>");
                            lines.Add(line);
                            break;
                        }

                stream.Close();
            }

            this.relevant_lines.InnerHtml = relevantCssLines.ToString();
        }
    }
}
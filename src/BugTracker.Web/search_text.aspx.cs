/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Text;
    using System.Web;
    using System.Web.UI;
    using Core;
    using Lucene.Net.Highlight;
    using Lucene.Net.Search;

    public partial class search_text : Page
    {
#pragma warning disable 618

        public Security security;

        public void Page_Load(object sender, EventArgs e)
        {
            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            Query query = null;

            try
            {
                if (string.IsNullOrEmpty(Request["query"]))
                    throw new Exception("You forgot to enter something to search for...");

                query = MyLucene.parser.Parse(Request["query"]);
            }
            catch (Exception e3)
            {
                display_exception(e3);
            }

            var scorer = new QueryScorer(query);
            var highlighter = new Highlighter(MyLucene.formatter, scorer);
            highlighter.SetTextFragmenter(MyLucene.fragmenter); // new Lucene.Net.Highlight.SimpleFragmenter(400));

            var sb = new StringBuilder();
            var guid = Guid.NewGuid().ToString().Replace("-", "");
            var dict_already_seen_ids = new Dictionary<string, int>();

            sb.Append(@"
create table #$GUID
(
temp_bg_id int,
temp_bp_id int,
temp_source varchar(30),
temp_score float,
temp_text nvarchar(3000)
)
    ");

            lock (MyLucene.my_lock)
            {
                Hits hits = null;
                try
                {
                    hits = MyLucene.search(query);
                }
                catch (Exception e2)
                {
                    display_exception(e2);
                }

                // insert the search results into a temp table which we will join with what's in the database
                for (var i = 0; i < hits.Length(); i++)
                    if (dict_already_seen_ids.Count < 100)
                    {
                        var doc = hits.Doc(i);
                        var bg_id = doc.Get("bg_id");
                        if (!dict_already_seen_ids.ContainsKey(bg_id))
                        {
                            dict_already_seen_ids[bg_id] = 1;
                            sb.Append("insert into #");
                            sb.Append(guid);
                            sb.Append(" values(");
                            sb.Append(bg_id);
                            sb.Append(",");
                            sb.Append(doc.Get("bp_id"));
                            sb.Append(",'");
                            sb.Append(doc.Get("src"));
                            sb.Append("',");
                            sb.Append(Convert.ToString(hits.Score(i))
                                .Replace(",", ".")); // Somebody said this fixes a bug. Localization issue?
                            sb.Append(",N'");

                            var raw_text = Server.HtmlEncode(doc.Get("raw_text"));
                            var stream = MyLucene.anal.TokenStream("", new StringReader(raw_text));
                            var highlighted_text =
                                highlighter.GetBestFragments(stream, raw_text, 1, "...").Replace("'", "''");
                            if (highlighted_text == "") // someties the highlighter fails to emit text...
                                highlighted_text = raw_text.Replace("'", "''");
                            if (highlighted_text.Length > 3000) highlighted_text = highlighted_text.Substring(0, 3000);
                            sb.Append(highlighted_text);
                            sb.Append("'");
                            sb.Append(")\n");
                        }
                    }
                    else
                    {
                        break;
                    }

                //searcher.Close();
            }

            sb.Append(@"
select '#ffffff', bg_id [id], bg_short_desc [desc] ,
	temp_source [search_source] ,
	temp_text [search_text],
	bg_reported_date [date],
	isnull(st_name,'') [status],
	temp_score [$SCORE]
from bugs
inner join #$GUID t on t.temp_bg_id = bg_id and t.temp_bp_id = 0
left outer join statuses on st_id = bg_status
where $ALTER_HERE

union

select '#ffffff', bg_id, bg_short_desc,
	bp_type + ',' + convert(varchar,bp_id) COLLATE DATABASE_DEFAULT,
	temp_text,
	bp_date,
	isnull(st_name,''),
	temp_score
from bugs
inner join #$GUID t on t.temp_bg_id = bg_id
inner join bug_posts on temp_bp_id = bp_id
left outer join statuses on st_id = bg_status
where $ALTER_HERE

order by t.temp_score desc, bg_id desc

drop table #$GUID

");

            var sql = sb.ToString().Replace("$GUID", guid);
            sql = Util.alter_sql_per_project_permissions(sql, this.security);

            var ds = DbUtil.get_dataset(sql);
            Session["bugs_unfiltered"] = ds.Tables[0];
            Session["bugs"] = new DataView(ds.Tables[0]);

            Session["just_did_text_search"] = "yes"; // switch for bugs.aspx
            Session["query"] = Request["query"]; // for util.cs, to persist the text in the search <input>
            Response.Redirect("bugs.aspx");
        }

        public void display_exception(Exception e)
        {
            var s = e.Message;
            if (e.InnerException != null)
            {
                s += "<br>";
                s += e.InnerException.Message;
            }

            Response.Write(@"
<html>
<link rel=StyleSheet href=btnet.css type=text/css>
<p>&nbsp;</p>
<div class=align>
<div class=err>");

            Response.Write(s);

            Response.Write(@"
<p>
<a href='javascript:history.go(-1)'>back</a>            
</div></div>
</html>");

            Response.End();
        }
    }
}
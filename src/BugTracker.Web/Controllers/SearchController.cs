/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Controllers
{
    using Core;
    using Lucene.Net.Highlight;
    using Lucene.Net.Search;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Text;
    using System.Web.Mvc;

    [Authorize]
    public class SearchController : Controller
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly ISecurity security;

        public SearchController(
            IApplicationSettings applicationSettings,
            ISecurity security)
        {
            this.applicationSettings = applicationSettings;
            this.security = security;
        }

        [HttpGet]
        public ActionResult SearchText(string query)
        {
            Query searchQuery;

            try
            {
                if (string.IsNullOrEmpty(query))
                {
                    throw new Exception("You forgot to enter something to search for...");
                }

                searchQuery = MyLucene.Parser.Parse(query);
            }
            catch (Exception ex)
            {
                var message = DisplayException(ex);

                return Content(message);
            }

            var scorer = new QueryScorer(searchQuery);
            var highlighter = new Highlighter(MyLucene.Formatter, scorer);

            highlighter.SetTextFragmenter(MyLucene.Fragmenter); // new Lucene.Net.Highlight.SimpleFragmenter(400));

            var sb = new StringBuilder();
            var guid = Guid.NewGuid().ToString().Replace("-", string.Empty);
            var dictAlreadySeenIds = new Dictionary<string, int>();

            sb.Append(@"
                create table #$GUID
                (
                temp_bg_id int,
                temp_bp_id int,
                temp_source varchar(30),
                temp_score float,
                temp_text nvarchar(3000)
                )");

            lock (MyLucene.MyLock)
            {
                Hits hits = null;

                try
                {
                    hits = MyLucene.Search(searchQuery);
                }
                catch (Exception ex)
                {
                    var message = DisplayException(ex);

                    return Content(message);
                }

                // insert the search results into a temp table which we will join with what's in the database
                for (var i = 0; i < hits.Length(); i++)
                {
                    if (dictAlreadySeenIds.Count < 100)
                    {
                        var doc = hits.Doc(i);
                        var bgId = doc.Get("bg_id");

                        if (!dictAlreadySeenIds.ContainsKey(bgId))
                        {
                            dictAlreadySeenIds[bgId] = 1;
                            sb.Append("insert into #");
                            sb.Append(guid);
                            sb.Append(" values(");
                            sb.Append(bgId);
                            sb.Append(",");
                            sb.Append(doc.Get("bp_id"));
                            sb.Append(",'");
                            sb.Append(doc.Get("src"));
                            sb.Append("',");
                            sb.Append(Convert.ToString(hits.Score(i))
                                .Replace(",", ".")); // Somebody said this fixes a bug. Localization issue?
                            sb.Append(",N'");

                            var rawText = Server.HtmlEncode(doc.Get("raw_text"));
                            var stream = MyLucene.Anal.TokenStream(string.Empty, new StringReader(rawText));
                            var highlightedText = highlighter.GetBestFragments(stream, rawText, 1, "...").Replace("'", "''");

                            if (string.IsNullOrEmpty(highlightedText)) // someties the highlighter fails to emit text...
                            {
                                highlightedText = rawText.Replace("'", "''");
                            }

                            if (highlightedText.Length > 3000)
                            {
                                highlightedText = highlightedText.Substring(0, 3000);
                            }

                            sb.Append(highlightedText);
                            sb.Append("'");
                            sb.Append(")\n");
                        }
                    }
                    else
                    {
                        break;
                    }
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

                drop table #$GUID");

            var sql = sb.ToString().Replace("$GUID", guid);

            sql = Util.AlterSqlPerProjectPermissions(sql, this.security);

            var ds = DbUtil.GetDataSet(sql);

            Session["bugs_unfiltered"] = ds.Tables[0];
            Session["bugs"] = new DataView(ds.Tables[0]);

            Session["just_did_text_search"] = "yes"; // switch for /Bug
            Session["query"] = query; // for util.cs, to persist the text in the search <input>

            return RedirectToAction("Index", "Bug");
        }

        private static string DisplayException(Exception e)
        {
            var stringBuilder = new StringBuilder();
            var message = e.Message;

            if (e.InnerException != null)
            {
                message += "<br>";
                message += e.InnerException.Message;
            }

            stringBuilder.Append(@"
                <html>
                <link rel=StyleSheet href=Content/btnet.css type=text/css>
                <p>&nbsp;</p>
                <div class=align>
                <div class=err>");

            stringBuilder.Append(message);

            stringBuilder.Append(@"
                <p>
                <a href='javascript:history.go(-1)'>back</a>
                </div></div>
                </html>");

            return stringBuilder.ToString();
        }
    }
}
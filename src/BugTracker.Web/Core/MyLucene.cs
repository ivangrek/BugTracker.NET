/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

#pragma warning disable 618

namespace BugTracker.Web.Core
{
    using System;
    using System.Data;
    using System.IO;
    using System.Threading;
    using System.Web;
    using Lucene.Net.Analysis.Standard;
    using Lucene.Net.Documents;
    using Lucene.Net.Search.Highlight;
    using Lucene.Net.Index;
    using Lucene.Net.QueryParsers;
    using Lucene.Net.Search;
    using Lucene.Net.Store;

    public static class MyLucene
    {
        private const int TotalHits = 10;
        private const Lucene.Net.Util.Version LuceneVersion = Lucene.Net.Util.Version.LUCENE_30;

        public static string IndexPath = Util.GetLuceneIndexFolder();

        public static StandardAnalyzer Anal = new StandardAnalyzer(LuceneVersion);
        public static QueryParser Parser = new QueryParser(LuceneVersion, "text", Anal);

        public static IFormatter Formatter = new SimpleHTMLFormatter(
            "<span class='mark'>",
            "</span>");

        public static SimpleFragmenter Fragmenter = new SimpleFragmenter(400);
        private static Searcher Searcher;

        public static object MyLock = new object(); // for a lock

        private static Document CreateDoc(int bugId, int postId, string source, string text)
        {
            // btnet.Util.WriteToLog("indexing " + Convert.ToString(bug_id));

            var doc = new Document();

            //Fields f = new Lucene.Net.Documents.Field(

            doc.Add(new Field(
                "bg_id",
                Convert.ToString(bugId),
                Field.Store.YES,
                Field.Index.NOT_ANALYZED));

            doc.Add(new Field(
                "bp_id",
                Convert.ToString(postId),
                Field.Store.YES,
                Field.Index.NOT_ANALYZED));

            doc.Add(new Field(
                "src",
                source,
                Field.Store.YES,
                Field.Index.NOT_ANALYZED));

            // For the highlighter, store the raw text
            doc.Add(new Field(
                "raw_text",
                text,
                Field.Store.YES,
                Field.Index.NOT_ANALYZED));

            doc.Add(new Field(
                "text",
                new StringReader(text)));

            return doc;
        }

        private static DataSet GetTextCustomCols()
        {
            var dsCustomFields = DbUtil.GetDataSet(@"
                /* get searchable cols */
                select sc.name
                from syscolumns sc
                inner join systypes st on st.xusertype = sc.xusertype
                inner join sysobjects so on sc.id = so.id
                where so.name = 'bugs'
                and st.[name] <> 'sysname'
                and sc.name not in ('rowguid',
                'bg_id',
                'bg_short_desc',
                'bg_reported_user',
                'bg_reported_date',
                'bg_project',
                'bg_org',
                'bg_category',
                'bg_priority',
                'bg_status',
                'bg_assigned_to_user',
                'bg_last_updated_user',
                'bg_last_updated_date',
                'bg_user_defined_attribute',
                'bg_project_custom_dropdown_value1',
                'bg_project_custom_dropdown_value2',
                'bg_project_custom_dropdown_value3',
                'bg_tags')
                and st.[name] in ('nvarchar','varchar')
                and sc.length > 30");

            return dsCustomFields;
        }

        private static string GetTextCustomColsNames(DataSet dsCustomFields)
        {
            var customCols = string.Empty;
            foreach (DataRow dr in dsCustomFields.Tables[0].Rows) customCols += "[" + (string)dr["name"] + "],";
            return customCols;
        }

        // create a new index
        private static void ThreadProcBuild()
        {
            lock (MyLock)
            {
                try
                {
                    Util.WriteToLog("started creating Lucene index using folder " + IndexPath);

                    var directory = FSDirectory.Open(IndexPath);
                    var writer = new IndexWriter(directory, Anal, true, IndexWriter.MaxFieldLength.UNLIMITED);

                    var sql = @"
                        select bg_id,
                        $custom_cols
                        isnull(bg_tags,'') bg_tags,
                        bg_short_desc
                        from bugs";
                    var dsTextCustomCols = GetTextCustomCols();

                    sql = sql.Replace("$custom_cols", GetTextCustomColsNames(dsTextCustomCols));

                    // index the bugs
                    var ds = DbUtil.GetDataSet(sql);

                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        // desc
                        writer.AddDocument(CreateDoc(
                            (int)dr["bg_id"],
                            0,
                            "desc",
                            (string)dr["bg_short_desc"]));

                        // tags
                        var tags = (string)dr["bg_tags"];
                        if (!string.IsNullOrEmpty(tags))
                            writer.AddDocument(CreateDoc(
                                (int)dr["bg_id"],
                                0,
                                "tags",
                                tags));

                        // custom text fields
                        foreach (DataRow drCustomCol in dsTextCustomCols.Tables[0].Rows)
                        {
                            var name = (string)drCustomCol["name"];
                            var val = Convert.ToString(dr[name]);
                            if (!string.IsNullOrEmpty(val))
                                writer.AddDocument(CreateDoc(
                                    (int)dr["bg_id"],
                                    0,
                                    name.Replace("'", "''"),
                                    val));
                        }
                    }

                    // index the bug posts
                    ds = DbUtil.GetDataSet(@"
                        select bp_bug, bp_id, 
                        isnull(bp_comment_search,bp_comment) [text] 
                        from bug_posts 
                        where bp_type <> 'update'
                        and bp_hidden_from_external_users = 0");

                    foreach (DataRow dr in ds.Tables[0].Rows)
                        writer.AddDocument(CreateDoc(
                            (int)dr["bp_bug"],
                            (int)dr["bp_id"],
                            "post",
                            (string)dr["text"]));

                    writer.Optimize();
                    writer.Close();
                    Util.WriteToLog("done creating Lucene index");
                }
                catch (Exception e)
                {
                    Util.WriteToLog("exception building Lucene index: " + e.Message);
                    Util.WriteToLog(e.StackTrace);
                }
            }
        }

        public static ScoreDoc[] Search(Query query)
        {
            var hits = Array.Empty<ScoreDoc>();

            lock (MyLock) // prevent contention between searches and writing?
            {
                if (Searcher == null)
                {
                    var directory = FSDirectory.Open(MyLucene.IndexPath);

                    Searcher = new IndexSearcher(directory);
                }

                hits = Searcher.Search(query, TotalHits).ScoreDocs;
            }

            return hits;
        }

        // update an existing index
        private static void ThreadProcUpdate(object obj)
        {
            // just to be safe, make the worker threads wait for each other
            //System.Console.Beep(540, 20);
            lock (MyLock) // prevent contention between searching and writing?
            {
                //System.Console.Beep(840, 20);
                try
                {
                    if (Searcher != null)
                    {
                        try
                        {
                            Searcher.Close();
                        }
                        catch (Exception e)
                        {
                            Util.WriteToLog("Exception closing lucene searcher:" + e.Message);
                            Util.WriteToLog(e.StackTrace);
                        }

                        Searcher = null;
                    }

                    var directory = FSDirectory.Open(IndexPath);
                    var writer = new IndexWriter(directory, Anal, false, IndexWriter.MaxFieldLength.UNLIMITED);

                    // same as buid, but uses "modifier" instead of write.
                    // uses additional "where" clause for bugid

                    var bugId = (int)obj;

                    Util.WriteToLog("started updating Lucene index using folder " + IndexPath);

                    writer.DeleteDocuments(new Term("bg_id", Convert.ToString(bugId)));

                    var sql = @"
                        select bg_id, 
                        $custom_cols
                        isnull(bg_tags,'') bg_tags,
                        bg_short_desc    
                        from bugs where bg_id = $bugid";

                    sql = sql.Replace("$bugid", Convert.ToString(bugId));

                    var dsTextCustomCols = GetTextCustomCols();

                    sql = sql.Replace("$custom_cols", GetTextCustomColsNames(dsTextCustomCols));

                    // index the bugs
                    var dr = DbUtil.GetDataRow(sql);

                    writer.AddDocument(CreateDoc(
                        (int)dr["bg_id"],
                        0,
                        "desc",
                        (string)dr["bg_short_desc"]));

                    // tags
                    var tags = (string)dr["bg_tags"];
                    if (!string.IsNullOrEmpty(tags))
                        writer.AddDocument(CreateDoc(
                            (int)dr["bg_id"],
                            0,
                            "tags",
                            tags));

                    // custom text fields
                    foreach (DataRow drCustomCol in dsTextCustomCols.Tables[0].Rows)
                    {
                        var name = (string)drCustomCol["name"];
                        var val = Convert.ToString(dr[name]);
                        if (!string.IsNullOrEmpty(val))
                            writer.AddDocument(CreateDoc(
                                (int)dr["bg_id"],
                                0,
                                name.Replace("'", "''"),
                                val));
                    }

                    // index the bug posts
                    var ds = DbUtil.GetDataSet(@"
                        select bp_bug, bp_id, 
                        isnull(bp_comment_search,bp_comment) [text] 
                        from bug_posts 
                        where bp_type <> 'update'
                        and bp_hidden_from_external_users = 0
                        and bp_bug = " + Convert.ToString(bugId));

                    foreach (DataRow dr2 in ds.Tables[0].Rows)
                        writer.AddDocument(CreateDoc(
                            (int)dr2["bp_bug"],
                            (int)dr2["bp_id"],
                            "post",
                            (string)dr2["text"]));

                    writer.Commit();
                    writer.Close();
                    Util.WriteToLog("done updating Lucene index");
                }
                catch (Exception e)
                {
                    Util.WriteToLog("exception updating Lucene index: " + e.Message);
                    Util.WriteToLog(e.StackTrace);
                }
            }
        }

        public static void BuildLuceneIndex()
        {
            var thread = new Thread(ThreadProcBuild);

            thread.Start();
        }

        public static void UpdateLuceneIndex(int bugId)
        {
            var thread = new Thread(ThreadProcUpdate);
            thread.Start(bugId);
        }
    }
}
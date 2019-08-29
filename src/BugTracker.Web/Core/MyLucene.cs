/*
    Copyright 2002-2011 Corey Trager

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
    using Lucene.Net.Highlight;
    using Lucene.Net.Index;
    using Lucene.Net.QueryParsers;
    using Lucene.Net.Search;

    public class MyLucene
    {
        public static string index_path = Util.get_lucene_index_folder();

        public static StandardAnalyzer anal = new StandardAnalyzer();
        public static QueryParser parser = new QueryParser("text", anal);

        public static Formatter formatter = new SimpleHTMLFormatter(
            "<span class='highlighted'>",
            "</span>");

        public static SimpleFragmenter fragmenter = new SimpleFragmenter(400);
        protected static Searcher searcher;

        public static object my_lock = new object(); // for a lock

        private static Document create_doc(int bug_id, int post_id, string source, string text)
        {
            // btnet.Util.write_to_log("indexing " + Convert.ToString(bug_id));

            var doc = new Document();

            //Fields f = new Lucene.Net.Documents.Field(

            doc.Add(new Field(
                "bg_id",
                Convert.ToString(bug_id),
                Field.Store.YES,
                Field.Index.UN_TOKENIZED));

            doc.Add(new Field(
                "bp_id",
                Convert.ToString(post_id),
                Field.Store.YES,
                Field.Index.UN_TOKENIZED));

            doc.Add(new Field(
                "src",
                source,
                Field.Store.YES,
                Field.Index.UN_TOKENIZED));

            // For the highlighter, store the raw text
            doc.Add(new Field(
                "raw_text",
                text,
                Field.Store.YES,
                Field.Index.UN_TOKENIZED));

            doc.Add(new Field(
                "text",
                new StringReader(text)));

            return doc;
        }

        private static DataSet get_text_custom_cols()
        {
            var ds_custom_fields = DbUtil.get_dataset(@"
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

            return ds_custom_fields;
        }

        private static string get_text_custom_cols_names(DataSet ds_custom_fields)
        {
            var custom_cols = "";
            foreach (DataRow dr in ds_custom_fields.Tables[0].Rows) custom_cols += "[" + (string) dr["name"] + "],";
            return custom_cols;
        }

        // create a new index
        private static void threadproc_build(object obj)
        {
            lock (my_lock)
            {
                try
                {
                    var app = (HttpApplicationState) obj;

                    Util.write_to_log("started creating Lucene index using folder " + index_path);
                    var writer = new IndexWriter(index_path, anal, true);

                    var sql = @"
select bg_id, 	
$custom_cols
isnull(bg_tags,'') bg_tags,
bg_short_desc
from bugs";
                    var ds_text_custom_cols = get_text_custom_cols();

                    sql = sql.Replace("$custom_cols", get_text_custom_cols_names(ds_text_custom_cols));

                    // index the bugs
                    var ds = DbUtil.get_dataset(sql);

                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        // desc
                        writer.AddDocument(create_doc(
                            (int) dr["bg_id"],
                            0,
                            "desc",
                            (string) dr["bg_short_desc"]));

                        // tags
                        var tags = (string) dr["bg_tags"];
                        if (tags != "")
                            writer.AddDocument(create_doc(
                                (int) dr["bg_id"],
                                0,
                                "tags",
                                tags));

                        // custom text fields
                        foreach (DataRow dr_custom_col in ds_text_custom_cols.Tables[0].Rows)
                        {
                            var name = (string) dr_custom_col["name"];
                            var val = Convert.ToString(dr[name]);
                            if (val != "")
                                writer.AddDocument(create_doc(
                                    (int) dr["bg_id"],
                                    0,
                                    name.Replace("'", "''"),
                                    val));
                        }
                    }

                    // index the bug posts
                    ds = DbUtil.get_dataset(@"
select bp_bug, bp_id, 
isnull(bp_comment_search,bp_comment) [text] 
from bug_posts 
where bp_type <> 'update'
and bp_hidden_from_external_users = 0");

                    foreach (DataRow dr in ds.Tables[0].Rows)
                        writer.AddDocument(create_doc(
                            (int) dr["bp_bug"],
                            (int) dr["bp_id"],
                            "post",
                            (string) dr["text"]));

                    writer.Optimize();
                    writer.Close();
                    Util.write_to_log("done creating Lucene index");
                }
                catch (Exception e)
                {
                    Util.write_to_log("exception building Lucene index: " + e.Message);
                    Util.write_to_log(e.StackTrace);
                }
            }
        }

        public static Hits search(Query query)
        {
            Hits hits = null;
            lock (my_lock) // prevent contention between searches and writing?
            {
                if (searcher == null) searcher = new IndexSearcher(index_path);
                hits = searcher.Search(query);
            }

            return hits;
        }

        // update an existing index
        private static void threadproc_update(object obj)
        {
            // just to be safe, make the worker threads wait for each other
            //System.Console.Beep(540, 20);
            lock (my_lock) // prevent contention between searching and writing?
            {
                //System.Console.Beep(840, 20);
                try
                {
                    if (searcher != null)
                    {
                        try
                        {
                            searcher.Close();
                        }
                        catch (Exception e)
                        {
                            Util.write_to_log("Exception closing lucene searcher:" + e.Message);
                            Util.write_to_log(e.StackTrace);
                        }

                        searcher = null;
                    }

                    var modifier = new IndexModifier(index_path, anal, false);

                    // same as buid, but uses "modifier" instead of write.
                    // uses additional "where" clause for bugid

                    var bug_id = (int) obj;

                    Util.write_to_log("started updating Lucene index using folder " + index_path);

                    modifier.DeleteDocuments(new Term("bg_id", Convert.ToString(bug_id)));

                    var sql = @"
select bg_id, 
$custom_cols
isnull(bg_tags,'') bg_tags,
bg_short_desc    
from bugs where bg_id = $bugid";

                    sql = sql.Replace("$bugid", Convert.ToString(bug_id));

                    var ds_text_custom_cols = get_text_custom_cols();

                    sql = sql.Replace("$custom_cols", get_text_custom_cols_names(ds_text_custom_cols));

                    // index the bugs
                    var dr = DbUtil.get_datarow(sql);

                    modifier.AddDocument(create_doc(
                        (int) dr["bg_id"],
                        0,
                        "desc",
                        (string) dr["bg_short_desc"]));

                    // tags
                    var tags = (string) dr["bg_tags"];
                    if (tags != "")
                        modifier.AddDocument(create_doc(
                            (int) dr["bg_id"],
                            0,
                            "tags",
                            tags));

                    // custom text fields
                    foreach (DataRow dr_custom_col in ds_text_custom_cols.Tables[0].Rows)
                    {
                        var name = (string) dr_custom_col["name"];
                        var val = Convert.ToString(dr[name]);
                        if (val != "")
                            modifier.AddDocument(create_doc(
                                (int) dr["bg_id"],
                                0,
                                name.Replace("'", "''"),
                                val));
                    }

                    // index the bug posts
                    var ds = DbUtil.get_dataset(@"
select bp_bug, bp_id, 
isnull(bp_comment_search,bp_comment) [text] 
from bug_posts 
where bp_type <> 'update'
and bp_hidden_from_external_users = 0
and bp_bug = " + Convert.ToString(bug_id));

                    foreach (DataRow dr2 in ds.Tables[0].Rows)
                        modifier.AddDocument(create_doc(
                            (int) dr2["bp_bug"],
                            (int) dr2["bp_id"],
                            "post",
                            (string) dr2["text"]));

                    modifier.Flush();
                    modifier.Close();
                    Util.write_to_log("done updating Lucene index");
                }
                catch (Exception e)
                {
                    Util.write_to_log("exception updating Lucene index: " + e.Message);
                    Util.write_to_log(e.StackTrace);
                }
            }
        }

        public static void build_lucene_index(HttpApplicationState app)
        {
            var thread = new Thread(threadproc_build);
            thread.Start(app);
        }

        public static void update_lucene_index(int bug_id)
        {
            var thread = new Thread(threadproc_update);
            thread.Start(bug_id);
        }
    }
}
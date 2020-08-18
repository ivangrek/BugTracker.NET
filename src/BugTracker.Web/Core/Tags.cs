/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text;
    using System.Threading;
    using System.Web;

    public static class Tags
    {
        public static string NormalizeTag(string s)
        {
            // Standardize the lower/upper case.  Initial cap, then the rest lower.
            var s2 = s.Trim().ToUpper();
            if (s2.Length > 1) s2 = s2[0] + s2.Substring(1).ToLower();
            return s2;
        }

        public static void ThreadProcVotes(object obj)
        {
            Util.WriteToLog("ThreadProcVotes");

            try
            {
                var app = (HttpApplicationState)obj;

                // Because "create view" wants to be the first in a batch, it won't work in setup.sql.
                // So let's just run it here every time.
                var sql = new SqlString(@"
                    if exists (select * from dbo.sysobjects where id = object_id(N'[votes_view]'))
                    drop view [votes_view]");

                DbUtil.ExecuteNonQuery(sql);

                sql = new SqlString(@"
                    create view votes_view as
                    select bu_bug as vote_bug, sum(bu_vote) as vote_total
                    from bug_user
                    group by bu_bug
                    having sum(bu_vote) > 0");

                DbUtil.ExecuteNonQuery(sql);

                sql = new SqlString(@"
                    select bu_bug, count(1)
                    from bug_user 
                    where bu_vote = 1
                    group by bu_bug");

                var ds = DbUtil.GetDataSet(sql);

                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    app[Convert.ToString(dr[0])] = (int) dr[1];
                }
            }
            catch (Exception ex)
            {
                Util.WriteToLog("exception in ThreadProcVotes:" + ex.Message);
            }
        }

        public static void ThreadProcTags()
        {
            try
            {
                var tags = new SortedDictionary<string, List<int>>();

                // update the cache

                var ds = DbUtil.GetDataSet(new SqlString("select bg_id, bg_tags from bugs where isnull(bg_tags,'') <> ''"));

                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    var labels = Util.SplitStringUsingCommas((string)dr[1]);

                    // for each tag label, build a list of bugids that have that label
                    for (var i = 0; i < labels.Length; i++)
                    {
                        var label = NormalizeTag(labels[i]);

                        if (!string.IsNullOrEmpty(label))
                        {
                            if (!tags.ContainsKey(label)) tags[label] = new List<int>();

                            tags[label].Add((int)dr[0]);
                        }
                    }
                }

                Util.MemoryTags = tags;
            }
            catch (Exception ex)
            {
                Util.WriteToLog("exception in ThreadProcTags:" + ex.Message);
            }
        }

        public static void CountVotes(HttpApplicationState app)
        {
            var thread = new Thread(ThreadProcVotes);
            thread.Start(app);
        }

        public static void BuildTagIndex()
        {
            var thread = new Thread(ThreadProcTags);
            thread.Start();
        }

        public static string BuildFilterClause(HttpApplicationState app, string selectedLabels)
        {
            var labels = Util.SplitStringUsingCommas(selectedLabels);
            var tags = Util.MemoryTags ?? new SortedDictionary<string, List<int>>();

            var sb = new StringBuilder();
            sb.Append(" and id in (");

            var firstTime = true;

            // loop through all the tags entered by the user, building a list of
            // bug ids that contain ANY of the tags.
            for (var i = 0; i < labels.Length; i++)
            {
                var label = NormalizeTag(labels[i]);

                if (tags.ContainsKey(label))
                {
                    var ids = tags[label];

                    for (var j = 0; j < ids.Count; j++)
                    {
                        if (firstTime)
                            firstTime = false;
                        else
                            sb.Append(",");

                        sb.Append(Convert.ToString(ids[j]));
                    } // end of loop through ids
                }
            } // end of loop through lables

            sb.Append(")");

            // filter the list so that it only displays bugs that have ANY of the entered tags
            return sb.ToString();
        }
    }
}
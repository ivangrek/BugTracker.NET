/*
    Copyright 2002-2011 Corey Trager

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

    public class Tags
    {
        public static string normalize_tag(string s)
        {
            // Standardize the lower/upper case.  Initial cap, then the rest lower.
            var s2 = s.Trim().ToUpper();
            if (s2.Length > 1) s2 = s2[0] + s2.Substring(1).ToLower();
            return s2;
        }

        public static void threadproc_votes(object obj)
        {
            Util.write_to_log("threadproc_votes");

            try
            {
                var app = (HttpApplicationState) obj;

                // Because "create view" wants to be the first in a batch, it won't work in setup.sql.
                // So let's just run it here every time.
                var sql = @"
if exists (select * from dbo.sysobjects where id = object_id(N'[votes_view]'))
drop view [votes_view]";

                DbUtil.execute_nonquery(sql);

                sql = @"
create view votes_view as
select bu_bug as vote_bug, sum(bu_vote) as vote_total
from bug_user
group by bu_bug
having sum(bu_vote) > 0";

                DbUtil.execute_nonquery(sql);

                sql = @"
select bu_bug, count(1)
from bug_user 
where bu_vote = 1
group by bu_bug";

                var ds = DbUtil.get_dataset(sql);

                foreach (DataRow dr in ds.Tables[0].Rows) app[Convert.ToString(dr[0])] = (int) dr[1];
            }
            catch (Exception ex)
            {
                Util.write_to_log("exception in threadproc_votes:" + ex.Message);
            }
        }

        public static void threadproc_tags(object obj)
        {
            try
            {
                var app = (HttpApplicationState) obj;

                var tags = new SortedDictionary<string, List<int>>();

                // update the cache

                var ds = DbUtil.get_dataset("select bg_id, bg_tags from bugs where isnull(bg_tags,'') <> ''");

                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    var labels = Util.split_string_using_commas((string) dr[1]);

                    // for each tag label, build a list of bugids that have that label
                    for (var i = 0; i < labels.Length; i++)
                    {
                        var label = normalize_tag(labels[i]);

                        if (label != "")
                        {
                            if (!tags.ContainsKey(label)) tags[label] = new List<int>();

                            tags[label].Add((int) dr[0]);
                        }
                    }
                }

                app["tags"] = tags;
            }
            catch (Exception ex)
            {
                Util.write_to_log("exception in threadproc_tags:" + ex.Message);
            }
        }

        public static void count_votes(HttpApplicationState app)
        {
            var thread = new Thread(threadproc_votes);
            thread.Start(app);
        }

        public static void build_tag_index(HttpApplicationState app)
        {
            var thread = new Thread(threadproc_tags);
            thread.Start(app);
        }

        public static string build_filter_clause(HttpApplicationState app, string selected_labels)
        {
            var labels = Util.split_string_using_commas(selected_labels);

            var tags = (SortedDictionary<string, List<int>>) app["tags"];

            var sb = new StringBuilder();
            sb.Append(" and id in (");

            var first_time = true;

            // loop through all the tags entered by the user, building a list of
            // bug ids that contain ANY of the tags.
            for (var i = 0; i < labels.Length; i++)
            {
                var label = normalize_tag(labels[i]);

                if (tags.ContainsKey(label))
                {
                    var ids = tags[label];

                    for (var j = 0; j < ids.Count; j++)
                    {
                        if (first_time)
                            first_time = false;
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
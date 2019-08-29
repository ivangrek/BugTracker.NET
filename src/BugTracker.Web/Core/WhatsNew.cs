/*
    Copyright 2002-2011 Corey Trager

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core
{
    using System;
    using System.Collections.Generic;
    using System.Web;

    public class WhatsNew
    {
        public const long ten_million = 10000000;

        private static readonly object mylock = new object();
        private static long prev_seconds;

        public static void add_news(int bugid, string desc, string action, Security security)
        {
            if (Util.get_setting("EnableWhatsNewPage", "0") == "1")
            {
                var seconds = DateTime.Now.Ticks / ten_million;
                if (seconds == prev_seconds) seconds++; // prevent dupes, even if we have to lie.
                prev_seconds = seconds;

                var bn = new BugNews();
                bn.seconds = seconds;
                bn.seconds_string = Convert.ToString(seconds);
                bn.bugid = Convert.ToString(bugid);
                bn.desc = desc;
                bn.action = action;
                bn.who = security.user.username;

                lock (mylock)
                {
                    var app = (HttpApplicationState)HttpRuntime.Cache["Application"];
                    var list = (List<BugNews>)app["whatsnew"];

                    // create the list if necessary
                    if (list == null)
                    {
                        list = new List<BugNews>();
                        app["whatsnew"] = list;
                    }

                    // Add the newest item
                    list.Add(bn);

                    // Trim the old items
                    var max = Convert.ToInt32(Util.get_setting("WhatsNewMaxItemsCount", "200"));
                    while (list.Count > max) list.RemoveAt(0);
                }
            }
        }
    }

    public class BugNews
    {
        public string action;
        public string bugid;
        public string desc;
        public long seconds;
        public string seconds_string;
        public string who;
    }
}
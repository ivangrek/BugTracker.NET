/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core
{
    using System;
    using System.Collections.Generic;
    using System.Web;

    public class WhatsNew
    {
        public const long TenMillion = 10000000;

        private static readonly object Mylock = new object();
        private static long _prevSeconds;

        public static void AddNews(int bugid, string desc, string action, Security security)
        {
            if (Util.GetSetting("EnableWhatsNewPage", "0") == "1")
            {
                var seconds = DateTime.Now.Ticks / TenMillion;
                if (seconds == _prevSeconds) seconds++; // prevent dupes, even if we have to lie.
                _prevSeconds = seconds;

                var bn = new BugNews();
                bn.Seconds = seconds;
                bn.SecondsString = Convert.ToString(seconds);
                bn.Bugid = Convert.ToString(bugid);
                bn.Desc = desc;
                bn.Action = action;
                bn.Who = security.User.Username;

                lock (Mylock)
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
                    var max = Convert.ToInt32(Util.GetSetting("WhatsNewMaxItemsCount", "200"));
                    while (list.Count > max) list.RemoveAt(0);
                }
            }
        }
    }

    public class BugNews
    {
        public string Action;
        public string Bugid;
        public string Desc;
        public long Seconds;
        public string SecondsString;
        public string Who;
    }
}
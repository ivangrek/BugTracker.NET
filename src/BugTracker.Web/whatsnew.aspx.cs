/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class WhatsNew : Page
    {
        /*

The server sends back the current time and a list of the 
bugs since the "since" value.   The data is formated as JSON.

{
"now" : 9999,
"news_list" : [ 
            {
                "seconds":12,
                "bugid": 34,
                "desc": "foo",
                "action": "add",
                "who" : "ctrager",
            },

            {
                "seconds":12,
                "bugid": 34,
                "desc": "foo",
                "action": "add",
                "who" : "ctrager",
            },
            
          ]

}

*/

        public IApplicationSettings ApplicationSettings { get; set; }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            if (!ApplicationSettings.EnableWhatsNewPage)
            {
                Response.Write("Sorry, Web.config EnableWhatsNewPage is set to 0");
                Response.End();
            }

            var sinceString = Request["since"];
            if (string.IsNullOrEmpty(sinceString)) sinceString = "0";

            var since = Convert.ToInt64(sinceString);

            Response.ContentType = "application/json";

            var json = new StringBuilder();

            json.Append("{");

            // The web server's time.  The client javascript will use this a a reference point.
            append_json_var_val(json, "now", Convert.ToString(DateTime.Now.Ticks / Core.WhatsNew.TenMillion));

            // Serialize an array of BugNews objects
            json.Append(",\"news_list\":[");

            var list = (List<BugNews>) Application["whatsnew"];

            var firstNews = true;
            if (list != null)
                for (var i = 0; i < list.Count; i++)
                {
                    var news = list[i];
                    if (news.Seconds > since)
                    {
                        if (firstNews)
                            firstNews = false;
                        else
                            json.Append(",");

                        // Serialize BugNews object
                        json.Append("{");
                        append_json_var_val(json, "seconds", news.SecondsString);
                        json.Append(",");
                        append_json_var_val(json, "bugid", news.Bugid);
                        json.Append(",");
                        append_json_var_val(json, "desc", HttpUtility.HtmlEncode(news.Desc));
                        json.Append(",");
                        append_json_var_val(json, "action", news.Action);
                        json.Append(",");
                        append_json_var_val(json, "who", news.Who);
                        json.Append("}");
                    }
                }

            json.Append("]}");

            Response.Write(json.ToString());
        }

        public string escape_for_json(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        public void append_json_var_val(StringBuilder json, string var, string val)
        {
            json.Append("\"");
            json.Append(var);
            json.Append("\":\"");
            json.Append(escape_for_json(val));
            json.Append("\"");
        }
    }
}
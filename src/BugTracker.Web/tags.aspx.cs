/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Collections.Generic;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class tags : Page
    {
        public Security security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);
        }

        public void print_tags()
        {
            if (this.security.user.category_field_permission_level == Security.PERMISSION_NONE) return;

            var tags =
                (SortedDictionary<string, List<int>>) Application["tags"];

            var tags_by_count = new List<TagLabel>();

            var fonts = new Dictionary<string, string>();

            foreach (var s in tags.Keys)
            {
                var tl = new TagLabel();
                tl.count = tags[s].Count;
                tl.label = s;
                tags_by_count.Add(tl);
            }

            tags_by_count.Sort(); // sort in descending count order

            float total = tags.Count;
            var so_far = 0.0F;
            var previous_count = -1;
            var previous_font = "";

            foreach (var tl in tags_by_count)
            {
                so_far++;

                if (tl.count == previous_count)
                    fonts[tl.label] = previous_font; // if same count, then same font
                else if (so_far / total < .1)
                    fonts[tl.label] = "24pt";
                else if (so_far / total < .2)
                    fonts[tl.label] = "22pt";
                else if (so_far / total < .3)
                    fonts[tl.label] = "20pt";
                else if (so_far / total < .4)
                    fonts[tl.label] = "18pt";
                else if (so_far / total < .5)
                    fonts[tl.label] = "16pt";
                else if (so_far / total < .6)
                    fonts[tl.label] = "14pt";
                else if (so_far / total < .7)
                    fonts[tl.label] = "12pt";
                else if (so_far / total < .8)
                    fonts[tl.label] = "10pt";
                else
                    fonts[tl.label] = "8pt";

                previous_font = fonts[tl.label];
                previous_count = tl.count;
            }

            foreach (var s in tags.Keys)
            {
                Response.Write("\n<a style='font-size:");
                Response.Write(fonts[s]);
                Response.Write(";' href='javascript:opener.append_tag(\"");

                Response.Write(s.Replace("'", "%27"));

                Response.Write("\")'>");

                Response.Write(s);

                Response.Write("(");
                Response.Write(tags[s].Count);
                Response.Write(")</a>&nbsp;&nbsp; ");
            }
        }

        public class TagLabel : IComparable<TagLabel>
        {
            public int count;
            public string label;

            public int CompareTo(TagLabel other)
            {
                if (this.count > other.count)
                    return -1;
                if (this.count < other.count)
                    return 1;
                return 0;
            }
        }
    }
}
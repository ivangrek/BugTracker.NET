/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Collections.Generic;
    using System.Web.UI;
    using Core;

    public partial class Tags : Page
    {
        public Security Security { get; set; }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.AnyUserOk);
        }

        public void print_tags(ISecurity security)
        {
            if (Security.User.CategoryFieldPermissionLevel == SecurityPermissionLevel.PermissionNone) return;

            var tags = (SortedDictionary<string, List<int>>) Application["tags"];

            var tagsByCount = new List<TagLabel>();

            var fonts = new Dictionary<string, string>();

            foreach (var s in tags.Keys)
            {
                var tl = new TagLabel();
                tl.Count = tags[s].Count;
                tl.Label = s;
                tagsByCount.Add(tl);
            }

            tagsByCount.Sort(); // sort in descending count order

            float total = tags.Count;
            var soFar = 0.0F;
            var previousCount = -1;
            var previousFont = "";

            foreach (var tl in tagsByCount)
            {
                soFar++;

                if (tl.Count == previousCount)
                    fonts[tl.Label] = previousFont; // if same count, then same font
                else if (soFar / total < .1)
                    fonts[tl.Label] = "24pt";
                else if (soFar / total < .2)
                    fonts[tl.Label] = "22pt";
                else if (soFar / total < .3)
                    fonts[tl.Label] = "20pt";
                else if (soFar / total < .4)
                    fonts[tl.Label] = "18pt";
                else if (soFar / total < .5)
                    fonts[tl.Label] = "16pt";
                else if (soFar / total < .6)
                    fonts[tl.Label] = "14pt";
                else if (soFar / total < .7)
                    fonts[tl.Label] = "12pt";
                else if (soFar / total < .8)
                    fonts[tl.Label] = "10pt";
                else
                    fonts[tl.Label] = "8pt";

                previousFont = fonts[tl.Label];
                previousCount = tl.Count;
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
            public int Count;
            public string Label;

            public int CompareTo(TagLabel other)
            {
                if (this.Count > other.Count)
                    return -1;
                if (this.Count < other.Count)
                    return 1;
                return 0;
            }
        }
    }
}
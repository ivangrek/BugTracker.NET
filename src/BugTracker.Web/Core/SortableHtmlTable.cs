/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core
{
    using System;
    using System.Data;
    using System.Text;
    using System.Web;

    public class SortableHtmlTable
    {
        public static void CreateNonSortableFromDataSet(
            HttpResponse r,
            DataSet ds)
        {
            CreateFromDataSet(
                r,
                ds,
                "",
                "",
                true, // html encode
                false); // write_column_headings_as_links
        }

        public static void CreateFromDataSet(
            HttpResponse r,
            DataSet ds,
            string editUrl,
            string deleteUrl)
        {
            CreateFromDataSet(r, ds, editUrl, deleteUrl, true);
        }

        public static void CreateFromDataSet(
            HttpResponse r,
            DataSet ds,
            string editUrl,
            string deleteUrl,
            bool htmlEncode,
            bool writeColumnHeadingsAsLinks)
        {
            CreateStartOfTable(r, writeColumnHeadingsAsLinks);
            CreateHeadings(r, ds, editUrl, deleteUrl, writeColumnHeadingsAsLinks);
            CreateBody(r, ds, editUrl, deleteUrl, htmlEncode);
            CreateEndOfTable(r);
        }

        public static void CreateFromDataSet(
            HttpResponse r,
            DataSet ds,
            string editUrl,
            string deleteUrl,
            bool htmlEncode)
        {
            CreateStartOfTable(r, true); // write_column_headings_as_links
            CreateHeadings(r, ds, editUrl, deleteUrl, true); // write_column_headings_as_links
            CreateBody(r, ds, editUrl, deleteUrl, htmlEncode);
            CreateEndOfTable(r);
        }

        public static void CreateStartOfTable(
            HttpResponse r, bool writeColumnHeadingsAsLinks)
        {
            if (writeColumnHeadingsAsLinks)
            {
                r.Write("\n<div id=wait class=please_wait>&nbsp;</div>\n");
                r.Write("<div class=click_to_sort>click on column headings to sort</div>\n");
            }

            r.Write("<div id=myholder>\n");
            r.Write("<table id=mytable border=1 class=datat>\n");
        }

        public static void CreateEndOfTable(
            HttpResponse r)
        {
            // data
            r.Write("</table>\n");
            r.Write("</div>\n");
            r.Write("<div id=sortedby>&nbsp;</div>\n");
        }

        // headings

        public static void CreateHeadings(
            HttpResponse r,
            DataSet ds,
            string editUrl,
            string deleteUrl,
            bool writeColumnHeadingsAsLinks)
        {
            r.Write("<tr>\n");

            var dbColumnCount = 0;

            foreach (DataColumn dc in ds.Tables[0].Columns)
            {
                if ((editUrl != "" || deleteUrl != "")
                    && dbColumnCount == ds.Tables[0].Columns.Count - 1)
                {
                    if (editUrl != "") r.Write("<td class=datah valign=bottom>edit</td>");
                    if (deleteUrl != "") r.Write("<td class=datah valign=bottom>delete</td>");
                }
                else
                {
                    // determine data type
                    var datatype = "";
                    if (Util.IsNumericDataType(dc.DataType))
                        datatype = "num";
                    else if (dc.DataType == typeof(DateTime))
                        datatype = "date";
                    else
                        datatype = "str";

                    r.Write("<td class=datah valign=bottom>\n");

                    if (dc.ColumnName.StartsWith("$no_sort_"))
                    {
                        r.Write(dc.ColumnName.Replace("$no_sort_", ""));
                    }
                    else
                    {
                        if (writeColumnHeadingsAsLinks)
                        {
                            var sortlink = "<a href='javascript: sort_by_col($col, \"$type\")'>";
                            sortlink = sortlink.Replace("$col", Convert.ToString(dbColumnCount));
                            sortlink = sortlink.Replace("$type", datatype);
                            r.Write(sortlink);
                        }

                        r.Write(dc.ColumnName);
                        if (writeColumnHeadingsAsLinks) r.Write("</a>");
                    }

                    //r.Write ("<br>"); // for debugging
                    //r.Write (dc.DataType);

                    r.Write("</td>\n");
                }

                dbColumnCount++;
            }

            r.Write("</tr>\n");
        }

        // body, data

        public static void CreateBody(
            HttpResponse r,
            DataSet ds,
            string editUrl,
            string deleteUrl,
            bool htmlEncode)
        {
            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                r.Write("\n<tr>");
                for (var i = 0; i < ds.Tables[0].Columns.Count; i++)
                {
                    var datatype = ds.Tables[0].Columns[i].DataType;

                    if ((editUrl != "" || deleteUrl != "")
                        && i == ds.Tables[0].Columns.Count - 1)
                    {
                        if (editUrl != "")
                            r.Write("<td class=datad><a href="
                                    + editUrl + dr[ds.Tables[0].Columns.Count - 1] + ">edit</a></td>");
                        if (deleteUrl != "")
                            r.Write("<td class=datad><a href="
                                    + deleteUrl + dr[ds.Tables[0].Columns.Count - 1] + ">delete</a></td>");
                    }
                    else
                    {
                        if (Util.IsNumericDataType(datatype))
                            r.Write("<td class=datad align=right>");
                        else
                            r.Write("<td class=datad>");

                        if (dr[i].ToString() == "")
                        {
                            r.Write("&nbsp;");
                        }
                        else
                        {
                            if (datatype == typeof(DateTime))
                            {
                                r.Write(Util.FormatDbDateTime(dr[i]));
                            }
                            else if (datatype == typeof(decimal))
                            {
                                r.Write(Util.FormatDbValue(Convert.ToDecimal(dr[i])));
                            }
                            else
                            {
                                if (htmlEncode)
                                    r.Write(HttpUtility.HtmlEncode(dr[i].ToString()));
                                else
                                    r.Write(dr[i]);
                            }
                        }

                        r.Write("</td>");
                    }
                }

                r.Write("</tr>\n");
            }
        }
    }
}

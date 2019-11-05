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
    using System.Web;

    public static class BugList
    {
        private static IApplicationSettings ApplicationSettings { get; set; } = new ApplicationSettings();

        private static string GetDistinctValsFromDataset(DataTable dt, int col)
        {
            var dict = new SortedDictionary<string, int>();

            foreach (DataRow row in dt.Rows) dict[Convert.ToString(row[col])] = 1;

            var vals = string.Empty;

            foreach (var s in dict.Keys)
            {
                if (!string.IsNullOrEmpty(vals)) vals += "|";

                vals += s;
            }

            return vals;
        }

        private static string GetBugListBugCountString(DataView dv)
        {
            if (dv.Count == dv.Table.Rows.Count)
                return "<br><br>"
                       + Convert.ToString(dv.Table.Rows.Count)
                       + " "
                       + ApplicationSettings.PluralBugLabel
                       + " returned by query<br>";
            return "<br><br>"
                   + "Showing "
                   + Convert.ToString(dv.Count)
                   + " out of "
                   + Convert.ToString(dv.Table.Rows.Count)
                   + " "
                   + ApplicationSettings.PluralBugLabel
                   + " returned by query<br>";
        }

        private static string GetBugListPagingString(DataView dv, ISecurity security, bool isPostBack,
            string newPage, ref int thisPage)
        {
            // format the text "page N of N:  1 2..."
            thisPage = 0;
            if (isPostBack)
            {
                thisPage = Convert.ToInt32(newPage);
                HttpContext.Current.Session["page"] = thisPage;
            }
            else
            {
                if (HttpContext.Current.Session["page"] != null) thisPage = (int)HttpContext.Current.Session["page"];
            }

            // how many pages to show all the rows?
            var totalPages = (dv.Count - 1) / security.User.BugsPerPage + 1;

            if (thisPage > totalPages - 1)
            {
                thisPage = 0;
                HttpContext.Current.Session["page"] = thisPage;
            }

            var pagingString = string.Empty;

            if (totalPages > 1)
            {
                // The "<"
                if (thisPage > 0)
                    pagingString += "<a href='javascript: on_page("
                                     + Convert.ToString(thisPage - 1)
                                     + ")'><b>&nbsp;&lt&lt&nbsp;</b></a>&nbsp;";

                // first page is "0", second page is "1", so add 1 for display purposes
                pagingString += "page&nbsp;"
                                 + Convert.ToString(thisPage + 1)
                                 + "&nbsp;of&nbsp;"
                                 + Convert.ToString(totalPages)
                                 + "&nbsp;";

                // The ">"
                if (thisPage < totalPages - 1)
                    pagingString += "<a href='javascript: on_page("
                                     + Convert.ToString(thisPage + 1)
                                     + ")'><b>&nbsp;&gt;&gt;&nbsp;</b></a>";

                pagingString += "&nbsp;&nbsp;&nbsp;";

                var left = thisPage - 16;
                if (left < 1)
                    left = 0;
                else
                    pagingString += "<a href='javascript: on_page(0)'>[first]</a>...&nbsp;";

                var right = left + 32;
                if (right > totalPages) right = totalPages;

                for (var i = left; i < right; i++)
                    if (thisPage == i)
                        pagingString += "[" + Convert.ToString(i + 1) + "]&nbsp;";
                    else
                        pagingString += "<a href='javascript: on_page("
                                         + Convert.ToString(i)
                                         + ")'>"
                                         + Convert.ToString(i + 1)
                                         + "</a>&nbsp;";

                if (right < totalPages)
                    pagingString += "&nbsp;...<a href='javascript: on_page("
                                     + Convert.ToString(totalPages - 1)
                                     + ")'>[last]</a>";
            }

            return pagingString;
        }

        private static string adjust_filter_val(string filterVal)
        {
            var s = filterVal.Replace("[$FLAG] =$$$red$$$", "[$FLAG] =1");
            s = s.Replace("[$FLAG] =$$$green$$$", "[$FLAG] =2");
            s = s.Replace("[$FLAG]<>$$$$$$", "[$FLAG] <>0");
            s = s.Replace("[$FLAG] =$$$$$$", "[$FLAG] =0");
            s = s.Replace("[$FLAG]<>$$$red$$$", "[$FLAG]<>1");
            s = s.Replace("[$FLAG]<>$$$green$$$", "[$FLAG]<>2");

            s = s.Replace("[$SEEN] =$$$no$$$", "[$SEEN] =1");
            s = s.Replace("[$SEEN] =$$$yes$$$", "[$SEEN] =0");

            return s;
        }

        public static void SortAndFilterBugListDataView(DataView dv, bool isPostBack,
            string actnVal,
            ref string filterVal,
            ref string sortVal,
            ref string prevSortVal,
            ref string prevDirVal)
        {
            if (dv == null) return;

            // remember filter
            if (!isPostBack)
            {
                if (HttpContext.Current.Session["filter"] != null)
                {
                    filterVal = (string)HttpContext.Current.Session["filter"];
                    try
                    {
                        dv.RowFilter = adjust_filter_val(filterVal).Replace("'", "''").Replace("$$$", "'");
                    }
                    catch (Exception)
                    {
                        // just in case a filter in the Session is incompatible
                    }
                }
            }
            else
            {
                HttpContext.Current.Session["filter"] = filterVal;
                try
                {
                    var filterString2 = adjust_filter_val(filterVal).Replace("'", "''").Replace("$$$", "'");
                    if (HttpContext.Current.Request["tags"] != null && !string.IsNullOrEmpty(HttpContext.Current.Request["tags"]))
                        filterString2 += Tags.BuildFilterClause(
                            HttpContext.Current.Application,
                            HttpContext.Current.Request["tags"]);

                    dv.RowFilter = filterString2;
                }
                catch (Exception)
                {
                    // just in case a filter in the Session is incompatible
                }
            }

            // Determine which column to sort
            // and toggle ASC  DESC

            if (actnVal == "sort")
            {
                var sortColumn = Convert.ToInt32(sortVal) + 1;
                var sortExpression = dv.Table.Columns[sortColumn].ColumnName;
                if (sortVal == prevSortVal)
                {
                    if (prevDirVal == "ASC")
                    {
                        prevDirVal = "DESC";
                        sortExpression += " DESC";
                    }
                    else
                    {
                        prevDirVal = "ASC";
                    }
                }
                else
                {
                    prevSortVal = sortVal;
                    prevDirVal = "ASC";
                }

                dv.Sort = sortExpression;
                HttpContext.Current.Session["sort"] = sortExpression;
            }
            else
            {
                // remember sort
                if (!isPostBack)
                    if (HttpContext.Current.Session["sort"] != null)
                        try
                        {
                            dv.Sort = (string)HttpContext.Current.Session["sort"];
                        }
                        catch (Exception)
                        {
                            // just in case a sort stored in Session is incompatible
                        }
            }
        }

        private static string MaybeNot(string op, string text)
        {
            if (op == "<>")
                return "NOT " + text;
            return text;
        }

        private static void DisplayBugListFilterSelect(
            HttpResponse response,
            string filterVal,
            string which,
            DataTable table,
            string dropdownVals,
            int col)
        {
            // determine what the selected item in the dropdown should be

            var selectedValue = "[no filter]";
            var op = " =";
            var somethingSelected = false;

            if (filterVal.IndexOf("66 = 66") > -1)
            {
                var pos = filterVal.IndexOf(which);
                if (pos != -1)
                {
                    // move past the variable
                    pos += which.Length;
                    pos += 5; // to move past the " =$$$" and the single quote
                    var pos2 = filterVal.IndexOf("$$$", pos); // find the trailing $$$
                    selectedValue = filterVal.Substring(pos, pos2 - pos);
                    op = filterVal.Substring(pos - 5, 2);
                }
            }

            if (string.IsNullOrEmpty(selectedValue))
            {
                if (op == " =")
                    selectedValue = "[none]";
                else
                    selectedValue = "[any]";
            }

            // at this point we have the selected value

            if (selectedValue == "[no filter]")
                response.Write("<select class=filter ");
            else
                response.Write("<select class=filter_selected ");

            response.Write(" id='sel_" + which + "' onchange='on_filter()'>");
            response.Write("<option>[no filter]</option>");

            if (which != "[$SEEN]")
            {
                if (selectedValue == "[none]")
                {
                    response.Write("<option selected value=''>[none]</option>");
                    somethingSelected = true;
                }
                else
                {
                    response.Write("<option value=''>[none]</option>");
                }
            }

            if (which != "[$SEEN]")
            {
                if (selectedValue == "[any]")
                {
                    response.Write("<option selected value=''>[any]</option>");
                    somethingSelected = true;
                }
                else
                {
                    response.Write("<option value=''>[any]</option>");
                }
            }

            if (dropdownVals != null)
            {
                var options = Util.SplitDropdownVals(dropdownVals);
                for (var i = 0; i < options.Length; i++)
                    if (selectedValue == options[i])
                    {
                        response.Write("<option selected>" + MaybeNot(op, options[i]) + "</option>");
                        somethingSelected = true;
                    }
                    else
                    {
                        response.Write("<option>" + options[i] + "</option>");
                    }
            }
            else
            {
                foreach (DataRow dr in table.Rows)
                    if (selectedValue == Convert.ToString(dr[col]))
                    {
                        response.Write("<option selected>" + MaybeNot(op, Convert.ToString(dr[col])) + "</option>");
                        somethingSelected = true;
                    }
                    else
                    {
                        response.Write("<option>" + Convert.ToString(dr[col]) + "</option>");
                    }
            }

            if (!somethingSelected)
                if (selectedValue != "[no filter]")
                    response.Write("<option selected>" + selectedValue + "</option>");

            response.Write("</select>");
        }

        private static string DisplayBugListFilterSelectInline(
            string filterVal,
            string which,
            DataTable table,
            string dropdownVals,
            int col)
        {
            var stringBuilder = new StringBuilder();
            
            // determine what the selected item in the dropdown should be
            var selectedValue = "[no filter]";
            var op = " =";
            var somethingSelected = false;

            if (filterVal.IndexOf("66 = 66") > -1)
            {
                var pos = filterVal.IndexOf(which);
                if (pos != -1)
                {
                    // move past the variable
                    pos += which.Length;
                    pos += 5; // to move past the " =$$$" and the single quote
                    var pos2 = filterVal.IndexOf("$$$", pos); // find the trailing $$$
                    selectedValue = filterVal.Substring(pos, pos2 - pos);
                    op = filterVal.Substring(pos - 5, 2);
                }
            }

            if (string.IsNullOrEmpty(selectedValue))
            {
                if (op == " =")
                    selectedValue = "[none]";
                else
                    selectedValue = "[any]";
            }

            // at this point we have the selected value

            if (selectedValue == "[no filter]")
                stringBuilder.Append("<select class=filter ");
            else
                stringBuilder.Append("<select class=filter_selected ");

            stringBuilder.Append(" id='sel_" + which + "' onchange='on_filter()'>");
            stringBuilder.Append("<option>[no filter]</option>");

            if (which != "[$SEEN]")
            {
                if (selectedValue == "[none]")
                {
                    stringBuilder.Append("<option selected value=''>[none]</option>");
                    somethingSelected = true;
                }
                else
                {
                    stringBuilder.Append("<option value=''>[none]</option>");
                }
            }

            if (which != "[$SEEN]")
            {
                if (selectedValue == "[any]")
                {
                    stringBuilder.Append("<option selected value=''>[any]</option>");
                    somethingSelected = true;
                }
                else
                {
                    stringBuilder.Append("<option value=''>[any]</option>");
                }
            }

            if (dropdownVals != null)
            {
                var options = Util.SplitDropdownVals(dropdownVals);
                for (var i = 0; i < options.Length; i++)
                    if (selectedValue == options[i])
                    {
                        stringBuilder.Append("<option selected>" + MaybeNot(op, options[i]) + "</option>");
                        somethingSelected = true;
                    }
                    else
                    {
                        stringBuilder.Append("<option>" + options[i] + "</option>");
                    }
            }
            else
            {
                foreach (DataRow dr in table.Rows)
                    if (selectedValue == Convert.ToString(dr[col]))
                    {
                        stringBuilder.Append("<option selected>" + MaybeNot(op, Convert.ToString(dr[col])) + "</option>");
                        somethingSelected = true;
                    }
                    else
                    {
                        stringBuilder.Append("<option>" + Convert.ToString(dr[col]) + "</option>");
                    }
            }

            if (!somethingSelected)
                if (selectedValue != "[no filter]")
                    stringBuilder.Append("<option selected>" + selectedValue + "</option>");

            stringBuilder.Append("</select>");

            return stringBuilder.ToString();
        }

        public static void DisplayBugListTagsLine(HttpResponse response, ISecurity security)
        {
            if (security.User.TagsFieldPermissionLevel == SecurityPermissionLevel.PermissionNone) return;

            response.Write("\n<p>Show only rows with the following tags:&nbsp;");
            response.Write(
                "<input class=txt size=40 name=tags_input id=tags_input onchange='javascript:on_tags_change()' value=\"");
            response.Write(HttpUtility.HtmlEncode(HttpContext.Current.Request["tags"]));
            response.Write("\">");
            response.Write("<a href='javascript:show_tags()'>&nbsp;&nbsp;select tags</a>");
            response.Write("<br><br>\n");
        }

        public static string DisplayBugListTagsLineInline(ISecurity security)
        {
            var stringBuilder = new StringBuilder();

            if (security.User.TagsFieldPermissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                return string.Empty;
            }

            stringBuilder.Append("\n<p>Show only rows with the following tags:&nbsp;");
            stringBuilder.Append("<input class=txt size=40 name=tags_input id=tags_input onchange='javascript:on_tags_change()' value=\"");
            stringBuilder.Append(HttpUtility.HtmlEncode(HttpContext.Current.Request["tags"]));
            stringBuilder.Append("\">");
            stringBuilder.Append("<a href='javascript:show_tags()'>&nbsp;&nbsp;select tags</a>");
            stringBuilder.Append("<br><br>\n");

            return stringBuilder.ToString();
        }

        private static void DisplayFilterSelect(HttpResponse response, string filterVal, string which,
            DataTable table)
        {
            DisplayBugListFilterSelect(
                response,
                filterVal,
                which,
                table,
                null,
                0);
        }

        private static string DisplayFilterSelectInline(string filterVal, string which,
            DataTable table)
        {
            return DisplayBugListFilterSelectInline(
                filterVal,
                which,
                table,
                null,
                0);
        }

        private static void DisplayFilterSelect(HttpResponse response, string filterVal, string which,
            DataTable table, int col)
        {
            DisplayBugListFilterSelect(
                response,
                filterVal,
                which,
                table,
                null,
                col);
        }

        private static string DisplayFilterSelectInline(string filterVal, string which,
            DataTable table, int col)
        {
            return DisplayBugListFilterSelectInline(
                filterVal,
                which,
                table,
                null,
                col);
        }

        private static void DisplayFilterSelect(HttpResponse response, string filterVal, string which,
            string dropdownVals)
        {
            DisplayBugListFilterSelect(
                response,
                filterVal,
                which,
                null,
                dropdownVals,
                0);
        }

        private static string DisplayFilterSelectInline(string filterVal, string which,
            string dropdownVals)
        {
            return DisplayBugListFilterSelectInline(
                filterVal,
                which,
                null,
                dropdownVals,
                0);
        }

        public static void DisplayBugs(
            bool showCheckbox,
            DataView dv,
            HttpResponse response,
            ISecurity security,
            string newPageVal,
            bool isPostBack,
            DataSet dsCustomCols,
            string filterVal
        )
        {
            var thisPage = 0;
            var pagingString = GetBugListPagingString(
                dv,
                security,
                isPostBack,
                newPageVal,
                ref thisPage);

            var bugCountString = GetBugListBugCountString(dv);

            response.Write("<table border=0 cellpadding=0 cellspacing=0 width=100%><tr><td align=left valign=top>");
            response.Write(pagingString);
            response.Write(
                "<td align=right valign=top><span class=smallnote>clicking while holding Ctrl key toggles \"NOT\" in a filter: \"NOT project 1\"</span></table>");
            response.Write("\n<table class=bugt border=1 ><tr>\n");

            ///////////////////////////////////////////////////////////////////
            // headings
            ///////////////////////////////////////////////////////////////////

            var dbColumnCount = 0;
            var descriptionColumn = -1;

            var searchDescColumn = -1;
            var searchSourceColumn = -1;
            var searchTextColumn = -1;

            foreach (DataColumn dc in dv.Table.Columns)
            {
                if (dbColumnCount == 0)
                {
                    // skip color/style

                    if (showCheckbox) response.Write("<td class=bugh><font size=0>sel</font>");
                }
                else if (dc.ColumnName == "$SCORE")
                {
                    // don't display the score, but the "union" and "order by" in the
                    // query forces us to include it as one of the columns
                }
                else
                {
                    response.Write("<td class=bugh>\n");
                    // sorting
                    var s = "<a href='javascript: on_sort($col)'>";
                    s = s.Replace("$col", Convert.ToString(dbColumnCount - 1));
                    response.Write(s);

                    if (dc.ColumnName == "$FLAG")
                    {
                        response.Write("flag");
                    }
                    else if (dc.ColumnName == "$SEEN")
                    {
                        response.Write("new");
                    }
                    else if (dc.ColumnName == "$VOTE")
                    {
                        response.Write("votes");
                    }
                    else if (dc.ColumnName.ToLower().IndexOf("desc") == 0)
                    {
                        // remember this column so that we can make it a link
                        descriptionColumn = dbColumnCount; // zero based here
                        response.Write(dc.ColumnName);
                    }
                    else if (dc.ColumnName == "search_desc")
                    {
                        searchDescColumn = dbColumnCount;
                        response.Write("desc");
                    }
                    else if (dc.ColumnName == "search_text")
                    {
                        searchTextColumn = dbColumnCount;
                        response.Write("context");
                    }
                    else if (dc.ColumnName == "search_source")
                    {
                        searchSourceColumn = dbColumnCount;
                        response.Write("text source");
                    }
                    else
                    {
                        response.Write(dc.ColumnName);
                    }

                    response.Write("</a>");
                    response.Write("\n");
                }

                dbColumnCount++;
            }

            response.Write("\n<tr>");

            ////////////////////////////////////////////////////////////////////
            /// filter row
            ////////////////////////////////////////////////////////////////////

            if (dsCustomCols == null) dsCustomCols = Util.GetCustomColumns();

            dbColumnCount = 0;
            var udfColumnName = ApplicationSettings.UserDefinedBugAttributeName;

            foreach (DataColumn dc in dv.Table.Columns)
            {
                var lowercaseColumnName = dc.ColumnName.ToLower();

                // skip color
                if (dbColumnCount == 0)
                {
                    if (showCheckbox) response.Write("<td class=bugf>&nbsp;");
                }
                else if (dc.ColumnName == "$SCORE")
                {
                    // skip
                }
                else
                {
                    response.Write("<td class=bugf> ");

                    if (dc.ColumnName == "$FLAG")
                    {
                        DisplayFilterSelect(response, filterVal, "[$FLAG]", "red|green");
                    }
                    else if (dc.ColumnName == "$SEEN")
                    {
                        DisplayFilterSelect(response, filterVal, "[$SEEN]", "yes|no");
                    }
                    else if (lowercaseColumnName == "project"
                             || lowercaseColumnName == "organization"
                             || lowercaseColumnName == "category"
                             || lowercaseColumnName == "priority"
                             || lowercaseColumnName == "status"
                             || lowercaseColumnName == "reported by"
                             || lowercaseColumnName == "assigned to"
                             || lowercaseColumnName == udfColumnName.ToLower())
                    {
                        var stringVals = GetDistinctValsFromDataset(
                            (DataTable)HttpContext.Current.Session["bugs_unfiltered"],
                            dbColumnCount);

                        DisplayFilterSelect(
                            response,
                            filterVal,
                            "[" + dc.ColumnName + "]",
                            stringVals);
                    }
                    else
                    {
                        var withFilter = false;
                        foreach (DataRow drcc in dsCustomCols.Tables[0].Rows)
                            if (dc.ColumnName.ToLower() == Convert.ToString(drcc["name"]).ToLower())
                            {
                                if ((string)drcc["dropdown type"] == "normal"
                                    || (string)drcc["dropdown type"] == "users")
                                {
                                    withFilter = true;

                                    var stringVals = GetDistinctValsFromDataset(
                                        (DataTable)HttpContext.Current.Session["bugs_unfiltered"],
                                        dbColumnCount);

                                    DisplayFilterSelect(
                                        response,
                                        filterVal,
                                        "[" + (string)drcc["name"] + "]",
                                        stringVals);
                                }

                                break;
                            }

                        if (!withFilter) response.Write("&nbsp");
                    }

                    response.Write("\n");
                }

                dbColumnCount++;
            }

            response.Write("\n");

            var classOrColor = "class=bugd";
            string colOne;

            ///////////////////////////////////////////////////////////////////
            // data
            ///////////////////////////////////////////////////////////////////
            var rowsThisPage = 0;
            var j = 0;

            foreach (DataRowView drv in dv)
            {
                // skip over rows prior to this page
                if (j < security.User.BugsPerPage * thisPage)
                {
                    j++;
                    continue;
                }

                // do not show rows beyond this page
                rowsThisPage++;
                if (rowsThisPage > security.User.BugsPerPage) break;

                var dr = drv.Row;

                var stringBugid = Convert.ToString(dr[1]);

                response.Write("\n<tr>");

                if (showCheckbox)
                {
                    response.Write("<td class=bugd><input type=checkbox name=");
                    response.Write(stringBugid);
                    response.Write(">");
                }

                for (var i = 0; i < dv.Table.Columns.Count; i++)
                    if (i == 0)
                    {
                        colOne = Convert.ToString(dr[0]);

                        if (string.IsNullOrEmpty(colOne))
                        {
                            classOrColor = "class=bugd";
                        }
                        else
                        {
                            if (colOne[0] == '#')
                                classOrColor = "class=bugd bgcolor=" + colOne;
                            else
                                classOrColor = "class=\"" + colOne + "\"";
                        }
                    }
                    else
                    {
                        if (dv.Table.Columns[i].ColumnName == "$SCORE")
                        {
                            // skip
                        }
                        else if (dv.Table.Columns[i].ColumnName == "$FLAG")
                        {
                            var flag = (int)dr[i];
                            var cls = "wht";
                            if (flag == 1) cls = "red";
                            else if (flag == 2) cls = "grn";

                            response.Write(
                                "<td class=bugd align=center><span title='click to flag/unflag this for yourself' class="
                                + cls
                                + " onclick='flag(this, "
                                + stringBugid
                                + ")'>&nbsp;</span>");
                        }
                        else if (dv.Table.Columns[i].ColumnName == "$SEEN")
                        {
                            var seen = (int)dr[i];
                            var cls = "old";
                            if (seen == 0) cls = "new";

                            response.Write("<td class=bugd align=center><span title='click to toggle new/old' class="
                                           + cls
                                           + " onclick='seen(this, "
                                           + stringBugid
                                           + ")'>&nbsp;</span>");
                        }
                        else if (dv.Table.Columns[i].ColumnName == "$VOTE")
                        {
                            // we're going to use a scheme here to represent both the total votes
                            // and this particular user's vote.

                            // We'll assume that there will never be more than 10,000 votes.
                            // So, we'll encode the vote vount as 10,000 * vote count, and
                            // we'll use the 1 column as the yes/no of this user.
                            // So...
                            //  30,001 means 3 votes, 1 from this user.
                            // 120,000 means 12 votes, 0 from this user.
                            // The purpose of this is so that we can sort the column by votes,
                            // but still color it by THIS user's vote.

                            var voteCount = 0;
                            var thisUsersVote = 0;
                            var magicNumber = 10000;

                            var val = (int)dr[i];
                            thisUsersVote = val % magicNumber;

                            var objVoteCount = HttpContext.Current.Application[stringBugid];
                            if (objVoteCount != null) voteCount = (int)objVoteCount;

                            dr[i] = voteCount * magicNumber + thisUsersVote;

                            var cls = "novote";
                            if (thisUsersVote == 1) cls = "yesvote";

                            response.Write("<td class=bugd align=right><span title='click to toggle your vote' class="
                                           + cls
                                           + " onclick='vote(this, "
                                           + stringBugid
                                           + ")'>" + Convert.ToString(voteCount) + "</span>");
                        }

                        else
                        {
                            var datatype = dv.Table.Columns[i].DataType;

                            if (Util.IsNumericDataType(datatype))
                                response.Write("<td " + classOrColor + " align=right>");
                            else
                                response.Write("<td " + classOrColor + " >");

                            // write the data
                            if (string.IsNullOrEmpty(dr[i].ToString()))
                            {
                                response.Write("&nbsp;");
                            }
                            else
                            {
                                if (datatype == typeof(DateTime))
                                {
                                    // Some columns we'd like both date and time, some just date,
                                    // so let's be clever and if the time is exactly midnight, space it out
                                    response.Write(Util.FormatDbDateTime(dr[i]));
                                }
                                else
                                {
                                    if (i == descriptionColumn)
                                    {
                                        // write description as a link
                                        response.Write(
                                            "<a onmouseover=on_mouse_over(this) onmouseout=on_mouse_out() href=" + VirtualPathUtility.ToAbsolute($"~/Bugs/Edit.aspx?id={stringBugid}") + ">");
                                        response.Write(HttpContext.Current.Server.HtmlEncode(dr[i].ToString()));
                                        response.Write("</a>");
                                    }
                                    else if (i == searchDescColumn)
                                    {
                                        // write description as a link
                                        response.Write(
                                            "<a onmouseover=on_mouse_over(this) onmouseout=on_mouse_out() href=" + VirtualPathUtility.ToAbsolute($"~/Bugs/Edit.aspx?id={stringBugid}") + ">");
                                        response.Write(dr[i].ToString()); // already encoded
                                        response.Write("</a>");
                                    }
                                    else if (i == searchSourceColumn)
                                    {
                                        var val = dr[i].ToString();
                                        if (string.IsNullOrEmpty(val))
                                        {
                                            response.Write("&nbsp;");
                                        }
                                        else
                                        {
                                            var parts = Util.SplitStringUsingCommas(val);

                                            if (parts.Length < 2)
                                            {
                                                response.Write(val);
                                            }
                                            else
                                            {
                                                response.Write("<a href=" + VirtualPathUtility.ToAbsolute("~/Bugs/Edit.aspx?id="));
                                                response.Write(stringBugid); // bg_id
                                                response.Write("#");
                                                response.Write(parts[1]); // bp_id, the post id
                                                response.Write(">");
                                                response.Write(parts[0]); // sent, received, comment
                                                response.Write(" #");
                                                response.Write(parts[1]);
                                                response.Write("</a>");
                                            }
                                        }
                                    }
                                    else if (i == searchTextColumn)
                                    {
                                        response.Write(dr[i].ToString()); // already encoded
                                    }
                                    else
                                    {
                                        response.Write(HttpContext.Current.Server.HtmlEncode(dr[i].ToString())
                                            .Replace("\n", "<br>"));
                                    }
                                }
                            }
                        }

                        response.Write("");
                    }

                response.Write("\n");

                j++;
            }

            response.Write("</table>");
            response.Write(pagingString);
            response.Write(bugCountString);
        }

        public static string DisplayBugsInline(
            bool showCheckbox,
            DataView dv,
            ISecurity security,
            string newPageVal,
            bool isPostBack,
            DataSet dsCustomCols,
            string filterVal
        )
        {
            var stringBuilder = new StringBuilder();
            
            var thisPage = 0;
            var pagingString = GetBugListPagingString(
                dv,
                security,
                isPostBack,
                newPageVal,
                ref thisPage);

            var bugCountString = GetBugListBugCountString(dv);

            stringBuilder.Append("<table border=0 cellpadding=0 cellspacing=0 width=100%><tr><td align=left valign=top>");
            stringBuilder.Append(pagingString);
            stringBuilder.Append(
                "<td align=right valign=top><span class='text-info'>clicking while holding Ctrl key toggles \"NOT\" in a filter: \"NOT project 1\"</span></table>");
            stringBuilder.Append("\n<div class='table-responsive'>");
            stringBuilder.Append("\n<table class='bugt table table-sm table-striped table-bordered'><tr>\n");

            ///////////////////////////////////////////////////////////////////
            // headings
            ///////////////////////////////////////////////////////////////////

            var dbColumnCount = 0;
            var descriptionColumn = -1;

            var searchDescColumn = -1;
            var searchSourceColumn = -1;
            var searchTextColumn = -1;

            foreach (DataColumn dc in dv.Table.Columns)
            {
                if (dbColumnCount == 0)
                {
                    // skip color/style

                    if (showCheckbox) stringBuilder.Append("<th class=bugh><font size=0>sel</font>");
                }
                else if (dc.ColumnName == "$SCORE")
                {
                    // don't display the score, but the "union" and "order by" in the
                    // query forces us to include it as one of the columns
                }
                else
                {
                    stringBuilder.Append("<th class=bugh>\n");
                    // sorting
                    var s = "<a href='javascript: on_sort($col)'>";
                    s = s.Replace("$col", Convert.ToString(dbColumnCount - 1));
                    stringBuilder.Append(s);

                    if (dc.ColumnName == "$FLAG")
                    {
                        stringBuilder.Append("flag");
                    }
                    else if (dc.ColumnName == "$SEEN")
                    {
                        stringBuilder.Append("new");
                    }
                    else if (dc.ColumnName == "$VOTE")
                    {
                        stringBuilder.Append("votes");
                    }
                    else if (dc.ColumnName.ToLower().IndexOf("desc") == 0)
                    {
                        // remember this column so that we can make it a link
                        descriptionColumn = dbColumnCount; // zero based here
                        stringBuilder.Append(dc.ColumnName);
                    }
                    else if (dc.ColumnName == "search_desc")
                    {
                        searchDescColumn = dbColumnCount;
                        stringBuilder.Append("desc");
                    }
                    else if (dc.ColumnName == "search_text")
                    {
                        searchTextColumn = dbColumnCount;
                        stringBuilder.Append("context");
                    }
                    else if (dc.ColumnName == "search_source")
                    {
                        searchSourceColumn = dbColumnCount;
                        stringBuilder.Append("text source");
                    }
                    else
                    {
                        stringBuilder.Append(dc.ColumnName);
                    }

                    stringBuilder.Append("</a>");
                    stringBuilder.Append("\n");
                }

                dbColumnCount++;
            }

            stringBuilder.Append("\n<tr>");

            ////////////////////////////////////////////////////////////////////
            /// filter row
            ////////////////////////////////////////////////////////////////////

            if (dsCustomCols == null) dsCustomCols = Util.GetCustomColumns();

            dbColumnCount = 0;
            var udfColumnName = ApplicationSettings.UserDefinedBugAttributeName;

            foreach (DataColumn dc in dv.Table.Columns)
            {
                var lowercaseColumnName = dc.ColumnName.ToLower();

                // skip color
                if (dbColumnCount == 0)
                {
                    if (showCheckbox) stringBuilder.Append("<td class=bugf>&nbsp;");
                }
                else if (dc.ColumnName == "$SCORE")
                {
                    // skip
                }
                else
                {
                    stringBuilder.Append("<td class=bugf> ");

                    if (dc.ColumnName == "$FLAG")
                    {
                        stringBuilder.Append(DisplayFilterSelectInline(filterVal, "[$FLAG]", "red|green"));
                    }
                    else if (dc.ColumnName == "$SEEN")
                    {
                        stringBuilder.Append(DisplayFilterSelectInline(filterVal, "[$SEEN]", "yes|no"));
                    }
                    else if (lowercaseColumnName == "project"
                             || lowercaseColumnName == "organization"
                             || lowercaseColumnName == "category"
                             || lowercaseColumnName == "priority"
                             || lowercaseColumnName == "status"
                             || lowercaseColumnName == "reported by"
                             || lowercaseColumnName == "assigned to"
                             || lowercaseColumnName == udfColumnName.ToLower())
                    {
                        var stringVals = GetDistinctValsFromDataset(
                            (DataTable)HttpContext.Current.Session["bugs_unfiltered"],
                            dbColumnCount);

                        stringBuilder.Append(DisplayFilterSelectInline(
                            filterVal,
                            "[" + dc.ColumnName + "]",
                            stringVals));
                    }
                    else
                    {
                        var withFilter = false;
                        foreach (DataRow drcc in dsCustomCols.Tables[0].Rows)
                            if (dc.ColumnName.ToLower() == Convert.ToString(drcc["name"]).ToLower())
                            {
                                if ((string)drcc["dropdown type"] == "normal"
                                    || (string)drcc["dropdown type"] == "users")
                                {
                                    withFilter = true;

                                    var stringVals = GetDistinctValsFromDataset(
                                        (DataTable)HttpContext.Current.Session["bugs_unfiltered"],
                                        dbColumnCount);

                                    stringBuilder.Append(DisplayFilterSelectInline(
                                        filterVal,
                                        "[" + (string)drcc["name"] + "]",
                                        stringVals));
                                }

                                break;
                            }

                        if (!withFilter) stringBuilder.Append("&nbsp");
                    }

                    stringBuilder.Append("\n");
                }

                dbColumnCount++;
            }

            stringBuilder.Append("\n");

            var classOrColor = "class=bugd";
            string colOne;

            ///////////////////////////////////////////////////////////////////
            // data
            ///////////////////////////////////////////////////////////////////
            var rowsThisPage = 0;
            var j = 0;

            foreach (DataRowView drv in dv)
            {
                // skip over rows prior to this page
                if (j < security.User.BugsPerPage * thisPage)
                {
                    j++;
                    continue;
                }

                // do not show rows beyond this page
                rowsThisPage++;
                if (rowsThisPage > security.User.BugsPerPage) break;

                var dr = drv.Row;

                var stringBugid = Convert.ToString(dr[1]);

                stringBuilder.Append("\n<tr>");

                if (showCheckbox)
                {
                    stringBuilder.Append("<td class=bugd><input type=checkbox name=");
                    stringBuilder.Append(stringBugid);
                    stringBuilder.Append(">");
                }

                for (var i = 0; i < dv.Table.Columns.Count; i++)
                    if (i == 0)
                    {
                        colOne = Convert.ToString(dr[0]);

                        if (string.IsNullOrEmpty(colOne))
                        {
                            classOrColor = "class=bugd";
                        }
                        else
                        {
                            if (colOne[0] == '#')
                                classOrColor = "class=bugd bgcolor=" + colOne;
                            else
                                classOrColor = "class=\"" + colOne + "\"";
                        }
                    }
                    else
                    {
                        if (dv.Table.Columns[i].ColumnName == "$SCORE")
                        {
                            // skip
                        }
                        else if (dv.Table.Columns[i].ColumnName == "$FLAG")
                        {
                            var flag = (int)dr[i];
                            var cls = "wht";
                            if (flag == 1) cls = "red";
                            else if (flag == 2) cls = "grn";

                            stringBuilder.Append(
                                "<td class=bugd align=center><span title='click to flag/unflag this for yourself' class="
                                + cls
                                + " onclick='flag(this, "
                                + stringBugid
                                + ")'>&nbsp;</span>");
                        }
                        else if (dv.Table.Columns[i].ColumnName == "$SEEN")
                        {
                            var seen = (int)dr[i];
                            var cls = "old";
                            if (seen == 0) cls = "new";

                            stringBuilder.Append("<td class=bugd align=center><span title='click to toggle new/old' class="
                                           + cls
                                           + " onclick='seen(this, "
                                           + stringBugid
                                           + ")'>&nbsp;</span>");
                        }
                        else if (dv.Table.Columns[i].ColumnName == "$VOTE")
                        {
                            // we're going to use a scheme here to represent both the total votes
                            // and this particular user's vote.

                            // We'll assume that there will never be more than 10,000 votes.
                            // So, we'll encode the vote vount as 10,000 * vote count, and
                            // we'll use the 1 column as the yes/no of this user.
                            // So...
                            //  30,001 means 3 votes, 1 from this user.
                            // 120,000 means 12 votes, 0 from this user.
                            // The purpose of this is so that we can sort the column by votes,
                            // but still color it by THIS user's vote.

                            var voteCount = 0;
                            var thisUsersVote = 0;
                            var magicNumber = 10000;

                            var val = (int)dr[i];
                            thisUsersVote = val % magicNumber;

                            var objVoteCount = HttpContext.Current.Application[stringBugid];
                            if (objVoteCount != null) voteCount = (int)objVoteCount;

                            dr[i] = voteCount * magicNumber + thisUsersVote;

                            var cls = "novote";
                            if (thisUsersVote == 1) cls = "yesvote";

                            stringBuilder.Append("<td class=bugd align=right><span title='click to toggle your vote' class="
                                           + cls
                                           + " onclick='vote(this, "
                                           + stringBugid
                                           + ")'>" + Convert.ToString(voteCount) + "</span>");
                        }

                        else
                        {
                            var datatype = dv.Table.Columns[i].DataType;

                            if (Util.IsNumericDataType(datatype))
                                stringBuilder.Append("<td " + classOrColor + " align=right>");
                            else
                                stringBuilder.Append("<td " + classOrColor + " >");

                            // write the data
                            if (string.IsNullOrEmpty(dr[i].ToString()))
                            {
                                stringBuilder.Append("&nbsp;");
                            }
                            else
                            {
                                if (datatype == typeof(DateTime))
                                {
                                    // Some columns we'd like both date and time, some just date,
                                    // so let's be clever and if the time is exactly midnight, space it out
                                    stringBuilder.Append(Util.FormatDbDateTime(dr[i]));
                                }
                                else
                                {
                                    if (i == descriptionColumn)
                                    {
                                        // write description as a link
                                        stringBuilder.Append(
                                            "<a onmouseover=on_mouse_over(this) onmouseout=on_mouse_out() href=" + VirtualPathUtility.ToAbsolute($"~/Bugs/Edit.aspx?id={stringBugid}") + ">");
                                        stringBuilder.Append(HttpContext.Current.Server.HtmlEncode(dr[i].ToString()));
                                        stringBuilder.Append("</a>");
                                    }
                                    else if (i == searchDescColumn)
                                    {
                                        // write description as a link
                                        stringBuilder.Append(
                                            "<a onmouseover=on_mouse_over(this) onmouseout=on_mouse_out() href=" + VirtualPathUtility.ToAbsolute($"~/Bugs/Edit.aspx?id={stringBugid}") + ">");
                                        stringBuilder.Append(dr[i].ToString()); // already encoded
                                        stringBuilder.Append("</a>");
                                    }
                                    else if (i == searchSourceColumn)
                                    {
                                        var val = dr[i].ToString();
                                        if (string.IsNullOrEmpty(val))
                                        {
                                            stringBuilder.Append("&nbsp;");
                                        }
                                        else
                                        {
                                            var parts = Util.SplitStringUsingCommas(val);

                                            if (parts.Length < 2)
                                            {
                                                stringBuilder.Append(val);
                                            }
                                            else
                                            {
                                                stringBuilder.Append("<a href=" + VirtualPathUtility.ToAbsolute("~/Bugs/Edit.aspx?id="));
                                                stringBuilder.Append(stringBugid); // bg_id
                                                stringBuilder.Append("#");
                                                stringBuilder.Append(parts[1]); // bp_id, the post id
                                                stringBuilder.Append(">");
                                                stringBuilder.Append(parts[0]); // sent, received, comment
                                                stringBuilder.Append(" #");
                                                stringBuilder.Append(parts[1]);
                                                stringBuilder.Append("</a>");
                                            }
                                        }
                                    }
                                    else if (i == searchTextColumn)
                                    {
                                        stringBuilder.Append(dr[i].ToString()); // already encoded
                                    }
                                    else
                                    {
                                        stringBuilder.Append(HttpContext.Current.Server.HtmlEncode(dr[i].ToString())
                                            .Replace("\n", "<br>"));
                                    }
                                }
                            }
                        }

                        stringBuilder.Append("");
                    }

                stringBuilder.Append("\n");

                j++;
            }

            stringBuilder.Append("</table>");
            stringBuilder.Append("</div>");
            stringBuilder.Append(pagingString);
            stringBuilder.Append(bugCountString);

            return stringBuilder.ToString();
        }
    }
}
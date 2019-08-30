<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="PrintBugs2.aspx.cs" Inherits="BugTracker.Web.PrintBugs2" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>

<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <style>
        a {
            text-decoration: underline;
        }

            a:visited {
                text-decoration: underline;
            }

            a:hover {
                text-decoration: underline;
            }
    </style>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <%

        var firstrow = true;

        if (this.Dv != null)
        {
            foreach (DataRowView drv in this.Dv)
            {
                if (!firstrow)
                    Response.Write("<hr STYLE='page-break-before: always'>");
                else
                    firstrow = false;

                var dr = Bug.get_bug_datarow(
                    (int)drv[1], this.Security);

                PrintBug.print_bug(Response, dr, this.Security,
                    false /* include style */, this.ImagesInline, this.HistoryInline,
                    true /*internal_posts */);
                ;
            }
        }
        else
        {
            if (this.Ds != null)
            {
                foreach (DataRow dr2 in this.Ds.Tables[0].Rows)
                {
                    if (!firstrow)
                        Response.Write("<hr STYLE='page-break-before: always'>");
                    else
                        firstrow = false;

                    var dr = Bug.get_bug_datarow(
                        (int)dr2[1], this.Security);

                    PrintBug.print_bug(Response, dr, this.Security,
                        false, // include style
                        this.ImagesInline, this.HistoryInline,
                        true); // internal_posts
                }
            }
            else
            {
                Response.Write("Please recreate the list before trying to print...");
                Response.End();
            }
        }
    %>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
</asp:Content>

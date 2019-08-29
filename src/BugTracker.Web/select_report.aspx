<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="select_report.aspx.cs" Inherits="BugTracker.Web.select_report" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <script type="text/javascript" src="sortable.js"></script>

    <script>
        function select_report(type, id) {
            opener.add_selected_report(type, id);
            window.close();
        }
    </script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div class="align">
        <p />

        <%
            if (this.ds.Tables[0].Rows.Count > 0)
                SortableHtmlTable.create_from_dataset(
                    Response, this.ds, "", "", false);
            else
                Response.Write("No reports in the database.");
        %>
    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
    <% Response.Write(Application["custom_footer"]); %>
</asp:Content>

<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Reports.aspx.cs" Inherits="BugTracker.Web.Reports" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <script type="text/javascript" src="Scripts/sortable.js"></script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
    <% this.Security.WriteMenu(Response, "reports"); %>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div class="align">
        </p>

        <% if (this.Security.User.IsAdmin || this.Security.User.CanEditReports)
            { %>
        <a href="EditReport.aspx">add new report</a>&nbsp;&nbsp;&nbsp;&nbsp;
        <% } %>

        <a href="Dashboard.aspx">dashboard</a>

        <%

            if (this.Ds.Tables[0].Rows.Count > 0)
                SortableHtmlTable.CreateFromDataSet(
                    Response, this.Ds, "", "", false);
            else
                Response.Write("No reports in the database.");
        %>
    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
    <% Response.Write(Application["custom_footer"]); %>
</asp:Content>

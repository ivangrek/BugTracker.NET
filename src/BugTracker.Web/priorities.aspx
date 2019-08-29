<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="priorities.aspx.cs" Inherits="BugTracker.Web.priorities" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <script type="text/javascript" language="JavaScript" src="sortable.js"></script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
    <% this.security.write_menu(Response, "admin"); %>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div class="align">
        <a href="edit_priority.aspx">add new priority</a>
        <p />
        <%

            if (this.ds.Tables[0].Rows.Count > 0)
                SortableHtmlTable.create_from_dataset(
                    Response, this.ds, "edit_priority.aspx?id=", "delete_priority.aspx?id=", false);
            else
                Response.Write("No priorities in the database.");
        %>
    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
    <% Response.Write(Application["custom_footer"]); %>
</asp:Content>
>
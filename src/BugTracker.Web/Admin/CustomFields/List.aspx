<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="List.aspx.cs" Inherits="BugTracker.Web.Administration.CustomFields.List" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Register TagPrefix="BugTracker" TagName="MainMenu" Src="~/Core/Controls/MainMenu.ascx" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <script type="text/javascript" src="<%= ResolveUrl("~/Scripts/sortable.js")%>"></script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
    <BugTracker:MainMenu runat="server" ID="MainMenu"/>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div class="main">
        <a href="<%= ResolveUrl("~/Admin/CustomFields/Add.aspx")%>">add new custom field</a>
        <p/>
        <%
            if (this.Ds.Tables[0].Rows.Count > 0)
                SortableHtmlTable.CreateFromDataSet(
                    Response, this.Ds, ResolveUrl("~/Admin/CustomFields/Edit.aspx?id="), ResolveUrl("~/Admin/CustomFields/Delete.aspx?id="));
            else
                Response.Write("No custom fields.");
        %>
    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
    <% Response.Write(Application["custom_footer"]); %>
</asp:Content>

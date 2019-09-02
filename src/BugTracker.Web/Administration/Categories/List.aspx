<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="List.aspx.cs" Inherits="BugTracker.Web.Administration.Categories.List" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <script type="text/javascript" src="<%= ResolveUrl("~/Scripts/sortable.js")%>"></script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
    <% this.Security.WriteMenu(Response, "admin"); %>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div class="align">
        <a href="<%= ResolveUrl("~/Administration/Categories/Edit.aspx")%>">add new category</a>

        <%
            if (this.Ds.Tables[0].Rows.Count > 0)
            {
                SortableHtmlTable.CreateFromDataSet(Response, this.Ds,
                    ResolveUrl("~/Administration/Categories/Edit.aspx?id="),
                    ResolveUrl("~/Administration/Categories/Delete.aspx?id="));
            }
            else
            {
                Response.Write("No categories in the database.");
            }
        %>
    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
    <% Response.Write(Application["custom_footer"]); %>
</asp:Content>

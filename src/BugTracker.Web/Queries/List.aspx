<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="List.aspx.cs" Inherits="BugTracker.Web.Queries.List" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <script type="text/javascript" src="Scripts/sortable.js"></script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
    <% this.Security.WriteMenu(Response, "queries"); %>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div class="align">

        <% if (this.Security.User.IsAdmin || this.Security.User.CanEditSql)
            { %>
        <table border="0" width="80%">
            <tr>
                <td align="left" valign="top">
                    <a href="<%= ResolveUrl("~/Queries/Edit.aspx") %>">add new query</a>
                <td align="right" valign="top">
                    <form runat="server">
                        <span class="lbl">show everybody's private queries:</span>
                        <asp:CheckBox ID="show_all" class="cb" runat="server" AutoPostBack="True" />
                    </form>
        </table>
        <%
            }
            else
            {
                Response.Write("<p>");
            }
        %>


        <%

            if (this.Ds.Tables[0].Rows.Count > 0)
                SortableHtmlTable.CreateFromDataSet(
                    Response, this.Ds, "", "", false);
            else
                Response.Write("No queries in the database.");
        %>
    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
    <% Response.Write(Application["custom_footer"]); %>
</asp:Content>

<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="queries.aspx.cs" Inherits="BugTracker.Web.queries" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <script type="text/javascript" src="sortable.js"></script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
    <% this.security.write_menu(Response, "queries"); %>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div class="align">

        <% if (this.security.user.is_admin || this.security.user.can_edit_sql)
            { %>
        <table border="0" width="80%">
            <tr>
                <td align="left" valign="top">
                    <a href="edit_query.aspx">add new query</a>
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

            if (this.ds.Tables[0].Rows.Count > 0)
                SortableHtmlTable.create_from_dataset(
                    Response, this.ds, "", "", false);
            else
                Response.Write("No queries in the database.");
        %>
    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
    <% Response.Write(Application["custom_footer"]); %>
</asp:Content>

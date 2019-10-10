<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="List.aspx.cs" Inherits="BugTracker.Web.Administration.Users.List" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Register TagPrefix="BugTracker" TagName="MainMenu" Src="~/Core/Controls/MainMenu.ascx" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <%--TODO
    <body onload="filter_changed()">--%>

    <script type="text/javascript" src="<%= ResolveUrl("~/Scripts/sortable.js")%>"></script>

    <script>

        function filter_changed() {
            el = document.getElementById("filter_users");

            if (el.value != "") {
                el.style.background = "yellow";
            } else {
                el.style.background = "white";
            }

        }
    </script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
    <BugTracker:MainMenu runat="server" ID="MainMenu"/>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div class="main">
        <table border="0" width="80%">
            <tr>
                <td align="left" valign="top">
                    <a href="<%= ResolveUrl("~/Administration/Users/Edit.aspx")%>">add new user </a>
                    <td align="right" valign="top">
                        <form runat="server">

                            <span class="lbl">Show only usernames starting with:</span>
                            <input type="text" runat="server" id="filter_users" class="txt" value="" onkeyup="filter_changed()" style="color: red;"/>
                            &nbsp;&nbsp;&nbsp;

                <span class="lbl">hide inactive users:</span>
                            <asp:CheckBox ID="hide_inactive_users" class="cb" runat="server" />

                            <input type="submit" class="btn" value="Refresh User List">
                        </form>
        </table>

        <%

            if (this.Ds.Tables[0].Rows.Count > 0)
                SortableHtmlTable.CreateFromDataSet(
                    Response, this.Ds, "", "", false);
            else
                Response.Write("No users to display.");
        %>
    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
    <% Response.Write(Application["custom_footer"]); %>
</asp:Content>

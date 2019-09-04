<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="BugTracker.Web.Dashboard" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Register TagPrefix="BugTracker" TagName="MainMenu" Src="~/Core/Controls/MainMenu.ascx" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <style>
        body {
            background: #ffffff;
        }

        .panel {
            background: #ffffff;
            border: 3px solid #cccccc;
            margin-bottom: 10px;
            padding: 10px;
        }

        iframe {
            border: 1px solid white;
            height: 300px;
            width: 90%;
        }
    </style>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
    <BugTracker:MainMenu runat="server" ID="MainMenu"/>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <% if (Security.User.IsGuest) /* no dashboard */
        { %>
    <span class="disabled_link">edit dashboard not available to "guest" user</span>
    <% }
        else
        { %>
    <a href="EditDashboard.aspx">edit dashboard</a>
    <% } %>


    <table border="0" cellspacing="0" cellpadding="10">
        <tr>
            <td valign="top">&nbsp;<br>

                <% write_column(1); %>

            <td valign="top">&nbsp;<br>

                <% write_column(2); %>
    </table>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
    <% Response.Write(Application["custom_footer"]); %>
</asp:Content>

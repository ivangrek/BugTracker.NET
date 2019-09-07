<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="CompleteRegistration.aspx.cs" Inherits="BugTracker.Web.Accounts.CompleteRegistration" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
    <table style="border-spacing: 0;">
        <tr>
            <% Response.Write(Application["custom_logo"]); %>
        </tr>
    </table>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div>
        <table style="border-spacing: 0; margin: 0 auto;">
            <tr>
                <td>
                    <div runat="server" class="err" id="msg">&nbsp;</div>
                    <p />
                    <a href="<%= ResolveUrl("~/Accounts/Login.aspx") %>">Go to login page</a>
                </td>
            </tr>
        </table>
    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
</asp:Content>

<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="LoginNt.aspx.cs" Inherits="BugTracker.Web.Accounts.LoginNt" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <h1>Configuration Problem
    </h1>
    <p>
        This page has not been properly configured for Windows Integrated Authentication.
        Please contact your web administrator.
    </p>
    <p>
        Windows Integrated Authentication requires that this page (Accounts/LoginNt.aspx) does not
        permit anonymous access and Windows Integrated Security is selected as the authentication
        protocol.
    </p>
    <p>
        <a href="<%= ResolveUrl("~/Account/Login?msg=configuration+problem") %>">Go to logon page.</a>
    </p>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
</asp:Content>

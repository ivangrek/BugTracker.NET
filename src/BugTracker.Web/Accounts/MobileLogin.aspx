<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" ValidateRequest="false" AutoEventWireup="true" CodeBehind="MobileLogin.aspx.cs" Inherits="BugTracker.Web.Accounts.MobileLogin" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <link rel="stylesheet" href="<%= ResolveUrl("~/Scripts/jquery/jquery.mobile-1.2.0.min.css") %>" />
    <link rel="stylesheet" href="<%= ResolveUrl("~/Content/mbtnet_base.css") %>" />
    <script src="<%= ResolveUrl("~/Scripts/jquery/jquery-1.8.2.min.js") %>"></script>
    <script src="<%= ResolveUrl("~/Scripts/jquery/jquery.mobile-1.2.0.min.js") %>"></script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div data-role="page">

        <div data-role="header">
            <h1 id="my_header" runat="server">Header</h1>
        </div>
        <!-- /header -->

        <div data-role="content">

            <form data-ajax="false" id="Form1" class="frm" runat="server">
                <div class="err" runat="server" id="msg">&nbsp;</div>
                <label>User:</label>
                <input runat="server" type="text" id="user" />
                <label>Password:</label>
                <input runat="server" type="password" id="pw" />
                <input data-role="button" id="Submit1" type="submit" value="Logon" runat="server" />
                <input type="hidden" id="mobile" name="mobile" value="1" />
            </form>

        </div>
        <!-- /content -->

    </div>
    <!-- /page -->
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
</asp:Content>

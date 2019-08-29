<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="print_bug.aspx.cs" Inherits="BugTracker.Web.print_bug" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <style>
        a {
            text-decoration: underline;
        }

            a:visited {
                text-decoration: underline;
            }

            a:hover {
                text-decoration: underline;
            }
    </style>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <%
        PrintBug.print_bug(Response, this.dr, this.security,
            false, // include style
            this.images_inline, this.history_inline,
            true); // internal_posts 
    %>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
</asp:Content>

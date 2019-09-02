<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Blame.aspx.cs" Inherits="BugTracker.Web.Versioning.Hg.Blame" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <p>
        <pre>
<% write_blame(this.BlameText); %>
</pre>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
</asp:Content>

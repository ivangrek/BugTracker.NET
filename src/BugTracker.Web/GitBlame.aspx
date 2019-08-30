<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="GitBlame.aspx.cs" Inherits="BugTracker.Web.GitBlame" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <p>
        <pre>
<table border="0" class="datat" cellspacing="0" cellpadding="0">
<tr>
<td class="datah">commit
<td class="datah">author
<td class="datah">text
<td class="datah">date
<% write_blame(this.BlameText); %>
</table>
</pre>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
</asp:Content>

<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="TasksFrame.aspx.cs" Inherits="BugTracker.Web.TasksFrame" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <script>
        function set_task_cnt(cnt) {
            opener.set_task_cnt(<% Response.Write(this.StringBugid); %>, cnt);
        }
    </script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <iframe width="100%" height="100%" frameborder="0" scrolling="yes" src="Tasks.aspx?bugid=" <% Response.Write(this.StringBugid); %> />
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
</asp:Content>

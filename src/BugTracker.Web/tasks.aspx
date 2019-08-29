<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="tasks.aspx.cs" Inherits="BugTracker.Web.tasks" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <%--TODO
    <body onload="body_on_load()">--%>

    <script type="text/javascript" src="sortable.js"></script>
    <script>

        function body_on_load() {
            parent.set_task_cnt(<% Response.Write(Convert.ToString(ds.Tables[0].Rows.Count)); %>);
        }

    </script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div class="align">
        Tasks for
        <%
            Response.Write(Util.get_setting("SingularBugLabel", "bug")
                           + " "
                           + Convert.ToString(bugid));
        %>
        <p>

            <% if (permission_level == Security.PERMISSION_ALL && (security.user.is_admin || security.user.can_edit_tasks))
                { %>
            <a href="edit_task.aspx?id=0&bugid=" <% Response.Write(Convert.ToString(bugid)); %>>add new task</a>
            &nbsp;&nbsp;&nbsp;&nbsp;
            <a target="_blank" href="tasks_all.aspx">view all tasks</a>
            &nbsp;&nbsp;&nbsp;&nbsp;
            <a target="_blank" href="tasks_all_excel.aspx">export all tasks to excel</a>
        <p>

            <% } %>

            <%
                if (ds.Tables[0].Rows.Count > 0)
                {
                    SortableHtmlTable.create_from_dataset(
                        Response, ds, "", "", false);
                }
                else
                {
                    Response.Write("No tasks.");
                }
            %>
    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
    <% Response.Write(Application["custom_footer"]); %>
</asp:Content>

<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Tasks.aspx.cs" Inherits="BugTracker.Web.Tasks" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <%--TODO
    <body onload="body_on_load()">--%>

    <script type="text/javascript" src="Scripts/sortable.js"></script>
    <script>

        function body_on_load() {
            parent.set_task_cnt(<% Response.Write(Convert.ToString(this.Ds.Tables[0].Rows.Count)); %>);
        }

    </script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div class="align">
        Tasks for
        <%
            Response.Write(Util.GetSetting("SingularBugLabel", "bug")
                           + " "
                           + Convert.ToString(this.Bugid));
        %>
        <p>

            <% if (this.PermissionLevel == Security.PermissionAll && (this.Security.User.IsAdmin || this.Security.User.CanEditTasks))
                { %>
            <a href="EditTask?id=0&bugid=" <% Response.Write(Convert.ToString(this.Bugid)); %>>add new task</a>
            &nbsp;&nbsp;&nbsp;&nbsp;
            <a target="_blank" href="TasksAll.aspx">view all tasks</a>
            &nbsp;&nbsp;&nbsp;&nbsp;
            <a target="_blank" href="TasksAllExcel.aspx">export all tasks to excel</a>
        <p>

            <% } %>

            <%
                if (this.Ds.Tables[0].Rows.Count > 0)
                {
                    SortableHtmlTable.CreateFromDataSet(
                        Response, this.Ds, "", "", false);
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

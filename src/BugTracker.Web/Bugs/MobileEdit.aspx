<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" ValidateRequest="false" AutoEventWireup="true" CodeBehind="MobileEdit.aspx.cs" Inherits="BugTracker.Web.Bugs.MobileEdit" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

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
    <div class="page" data-role="page" data-cache="never">
        <div data-role="header">
            <h1 id="my_header" runat="server">BugTracker.NET Edit</h1>
        </div>
        <!-- /header -->

        <div data-role="content">
            <a class="ui-submit" data-ajax="false" href="<%= ResolveUrl("~/Bugs/MobileList.aspx") %>" data-role="button" data-icon="arrow-l" data-iconpos="left">Back to List</a>

            <form data-ajax="false" id="Form1" class="frm" runat="server">
                <div class="err" runat="server" id="msg">&nbsp;</div>

                <label>Description:</label>
                <textarea runat="server" id="short_desc" maxlength="200"></textarea>

                <label>Project:</label>
                <asp:DropDownList ID="project" runat="server"></asp:DropDownList>

                <label>Assigned to:</label>
                <asp:DropDownList ID="assigned_to" runat="server"></asp:DropDownList>

                <label>Status:</label>
                <asp:DropDownList ID="status" runat="server"></asp:DropDownList>

                <label>Comment:</label>
                <textarea id="comment" runat="server"></textarea>
                <input data-role="button" id="submit_button" type="submit" value="Button" runat="server"/>

                <% if (this.Id != 0)
                    { %>
                <br />
                <div>Reported by <span id="created_by" runat="server"></span></div>
                <% } %>

                <input type="hidden" id="prev_status" runat="server" />
                <input type="hidden" id="prev_short_desc" runat="server" />
                <input type="hidden" id="prev_assigned_to" runat="server" />
                <input type="hidden" id="prev_assigned_to_username" runat="server" />
                <input type="hidden" id="prev_project" runat="server" />
                <input type="hidden" id="prev_project_name" runat="server" />
            </form>

            <div id="posts">

                <%
                    // COMMENTS
                    if (this.Id != 0)
                        PrintBug.WritePosts(this.DsPosts,
                            Response, this.Id, this.PermissionLevel,
                            false, // write links
                            false, // images inline
                            true, // history inline
                            false, // internal_posts
                            Security.User);
                %>
            </div>

        </div>
        <!-- /content -->

    </div>
    <!-- /page -->
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
</asp:Content>

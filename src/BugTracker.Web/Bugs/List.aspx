<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="List.aspx.cs" Inherits="BugTracker.Web.Bugs.List" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>

<%@ Import Namespace="BugTracker.Web.Core" %>
<%@ Register Src="~/Core/Controls/MainMenu.ascx" TagPrefix="BugTracker" TagName="MainMenu" %>


<asp:Content ContentPlaceHolderID="Head" runat="server">
    <script type="text/javascript" src="<%= ResolveUrl("~/Scripts/bug_list.js") %>"></script>
    <script>
        $(document).ready(function () {
            $('.filter').click(on_invert_filter);
            $('.filter_selected').click(on_invert_filter);
        });

        function on_query_changed() {
            var frm = document.getElementById(asp_form_id);
            frm.actn.value = "query";
            frm.submit();
        }
    </script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
    <BugTracker:MainMenu runat="server" ID="MainMenu"/>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <form method="POST" runat="server">
        <div class="main">
            <table border="0">
                <tr>
                    <td nowrap>
                        <% if (!Security.User.AddsNotAllowed)
                            { %>
                        <a href="<%= ResolveUrl("~/Bugs/Edit.aspx") %>">
                            <img src="<%= ResolveUrl("~/Content/images/add.png") %>" border="0" align="top">&nbsp;add new <% Response.Write(ApplicationSettings.SingularBugLabel); %></a>
                        &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
            <% } %>

                    <td nowrap>
                        <asp:DropDownList ID="query" runat="server" onchange="on_query_changed()">
                        </asp:DropDownList>

                    <td nowrap>&nbsp;&nbsp;&nbsp;&nbsp;<a target="_blank" href="<%= ResolveUrl("~/Bugs/Print.aspx") %>">print list</a>
                    <td nowrap>&nbsp;&nbsp;&nbsp;&nbsp;<a target="_blank" href="<%= ResolveUrl("~/Bugs/Print2.aspx") %>">print detail</a>
                    <td nowrap>&nbsp;&nbsp;&nbsp;&nbsp;<a target="_blank" href="<%= ResolveUrl("~/Bugs/Print.aspx?format=excel") %>">export to excel</a>
                    <td nowrap align="right" width="100%">
                        <a target="_blank" href="<%= ResolveUrl("~/Content/btnet_screen_capture.exe") %>">
                            <img src="<%= ResolveUrl("~/Content/images/camera.png") %>" border="0" align="top">&nbsp;download screen capture utility</a>
            </table>
            <br>
            <%
                if (this.Dv != null)
                {
                    if (this.Dv.Table.Rows.Count > 0)
                    {
                        if (ApplicationSettings.EnableTags)
                        {
                            BugList.DisplayBugListTagsLine(Response, Security);
                        }
                        display_bugs(false, Security);
                    }
                    else
                    {
                        Response.Write("<p>No ");
                        Response.Write(ApplicationSettings.PluralBugLabel);
                        Response.Write(" yet.<p>");
                    }
                }
                else
                {
                    Response.Write("<div class=err>Error in query SQL: " + this.SqlError + "</div>");
                }
            %>
            <input type="hidden" name="new_page" id="new_page" runat="server" value="0" />
            <input type="hidden" name="actn" id="actn" runat="server" value="" />
            <input type="hidden" name="filter" id="filter" runat="server" value="" />
            <input type="hidden" name="sort" id="sort" runat="server" value="-1" />
            <input type="hidden" name="prev_sort" id="prev_sort" runat="server" value="-1" />
            <input type="hidden" name="prev_dir" id="prev_dir" runat="server" value="ASC" />
            <input type="hidden" name="tags" id="tags" value="">

            <script>
                var enable_popups = <% Response.Write(Security.User.EnablePopups ? "1" : "0"); %>;
                var asp_form_id = '<% Response.Write(ApplicationSettings.AspNetFormId); %>';
            </script>

            <div id="popup" class="buglist_popup"></div>
        </div>
    </form>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
    <% Response.Write(Application["custom_footer"]); %>
</asp:Content>

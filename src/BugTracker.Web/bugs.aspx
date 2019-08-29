﻿<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="bugs.aspx.cs" Inherits="BugTracker.Web.bugs" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <script type="text/javascript" src="bug_list.js"></script>
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
    <% security.write_menu(Response, Util.get_setting("PluralBugLabel", "bugs")); %>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <form method="POST" runat="server">
        <div class="align">
            <table border="0">
                <tr>
                    <td nowrap>
                        <% if (!security.user.adds_not_allowed)
                            { %>
                        <a href="edit_bug.aspx">
                            <img src="add.png" border="0" align="top">&nbsp;add new <% Response.Write(Util.get_setting("SingularBugLabel", "bug")); %></a>
                        &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
            <% } %>

                    <td nowrap>
                        <asp:DropDownList ID="query" runat="server" onchange="on_query_changed()">
                        </asp:DropDownList>

                    <td nowrap>&nbsp;&nbsp;&nbsp;&nbsp;<a target="_blank" href="print_bugs.aspx">print list</a>
                    <td nowrap>&nbsp;&nbsp;&nbsp;&nbsp;<a target="_blank" href="print_bugs2.aspx">print detail</a>
                    <td nowrap>&nbsp;&nbsp;&nbsp;&nbsp;<a target="_blank" href="print_bugs.aspx?format=excel">export to excel</a>
                    <td nowrap align="right" width="100%">
                        <a target="_blank" href="btnet_screen_capture.exe">
                            <img src="camera.png" border="0" align="top">&nbsp;download screen capture utility</a>
            </table>
            <br>
            <%
                if (dv != null)
                {
                    if (dv.Table.Rows.Count > 0)
                    {
                        if (Util.get_setting("EnableTags", "0") == "1")
                        {
                            BugList.display_buglist_tags_line(Response, security);
                        }
                        display_bugs(false);
                    }
                    else
                    {
                        Response.Write("<p>No ");
                        Response.Write(Util.get_setting("PluralBugLabel", "bugs"));
                        Response.Write(" yet.<p>");
                    }
                }
                else
                {
                    Response.Write("<div class=err>Error in query SQL: " + sql_error + "</div>");
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
                var enable_popups = <% Response.Write(security.user.enable_popups ? "1" : "0"); %>;
                var asp_form_id = '<% Response.Write(Util.get_form_name()); %>';
            </script>

            <div id="popup" class="buglist_popup"></div>
        </div>
    </form>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
    <% Response.Write(Application["custom_footer"]); %>
</asp:Content>

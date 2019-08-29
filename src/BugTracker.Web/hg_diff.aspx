<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="hg_diff.aspx.cs" Inherits="BugTracker.Web.hg_diff" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <%--TODO <body style="margin-right: 3px;">--%>

    <script type="text/javascript" language="JavaScript" src="jquery/jquery-1.3.2.min.js"></script>
    <script>

        left_size = .46;
        right_size = .46;

        $(document).ready(do_doc_ready);

        function do_doc_ready() {
            $(window).bind('resize', split);
            split();
        }

        function split() {
            $('#left').width($(window).width() * left_size);
            $('#right').width($(window).width() * right_size);

            if (left_size == right_size) {
                $('#show_left').css('display', 'block');
                $('#show_right').css('display', 'block');
                $('#show_both').css('display', 'none');
            } else if (left_size > right_size) {
                $('#show_left').css('display', 'none');
                $('#show_right').css('display', 'block');
                $('#show_both').css('display', 'block');
            } else if (left_size < right_size) {
                $('#show_left').css('display', 'block');
                $('#show_right').css('display', 'none');
                $('#show_both').css('display', 'block');
            }

        }

        function my_resize(l, r) {
            left_size = l;
            right_size = r;
            split();
        }
    </script>

    <style>
        .show_pane_button {
            background: #CCCCCC;
            border-bottom: 1px solid black;
            border-right: 1px solid black;
            margin-top: 3px;
            padding: 2px;
        }
    </style>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <p>
        <span class="diffadd" style="border: 1px solid black;">&nbsp;&nbsp;&nbsp;&nbsp;</span> = added,&nbsp;
    <span class="diffdel" style="border: 1px solid black;">&nbsp;&nbsp;&nbsp;&nbsp;</span> = deleted,&nbsp;
    <span class="diffchg" style="border: 1px solid black;">&nbsp;&nbsp;&nbsp;&nbsp;</span> = changed,&nbsp;

    <table border="0" width="97%">
        <tr>

            <td valign="top">
                <div style="border: 1px solid gray; margin: 3px; padding: 4px;">
                    <div class="difftitle">
                        <% Response.Write(this.left_title); %>
                    </div>
                    <pre id="left" style="overflow-x: auto;">
<% Response.Write(this.left_out); %>
              </pre>
                </div>

                <td valign="top">
                    <div id="show_left" class="show_pane_button">
                        <a href="javascript:my_resize(0.90,0.02)">show left</a>
                    </div>
                    <div id="show_right" class="show_pane_button">
                        <a href="javascript:my_resize(0.02,0.90)">show right</a>
                    </div>
                    <div id="show_both" class="show_pane_button">
                        <a href="javascript:my_resize(0.46,0.46)">show both</a>
                    </div>
                </td>

            <td valign="top">
                <div style="border: 1px solid gray; margin: 3px; padding: 4px;">
                    <div class="difftitle">
                        <% Response.Write(this.right_title); %>
                    </div>
                    <pre id="right" style="overflow-x: auto;">
<% Response.Write(this.right_out); %>
                </pre>
                </div>

    </table>

        <p>
            <script>
                function show_raw_diff() {
                    el = document.getElementById("raw_diff");
                    el.style.display = "block";
                }
            </script>

            <a href="javascript: show_raw_diff()">show raw unified diff text</a>
            <pre id="raw_diff" style="display: none;">
          <% Response.Write(HttpUtility.HtmlEncode(this.unified_diff_text)); %>
        </pre>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
</asp:Content>


<html>
<title>hg diff <% Response.Write(HttpUtility.HtmlEncode(this.path)); %></title>

<link rel="StyleSheet" href="btnet.css" type="text/css">
</body>
</html>

<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="edit_dashboard.aspx.cs" Inherits="BugTracker.Web.edit_dashboard" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <style>
        body {
            background: #ffffff;
        }

        .panel {
            background: #ffffff;
            border: 3px solid #cccccc;
            margin-bottom: 10px;
            padding: 10px;
        }
    </style>

    <script>
        var col = 0;

        function show_select_report_page(which_col) {

            col = which_col;
            popup_window = window.open('select_report.aspx');
        }

        function add_selected_report(chart_type, id) {
            var frm = document.getElementById("addform");
            frm.rp_chart_type.value = chart_type;
            frm.rp_id.value = id;
            frm.rp_col.value = col;
            frm.submit();
        }
    </script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
    <% this.security.write_menu(Response, "admin"); %>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <a href="dashboard.aspx">back to dashboard</a>
    <table border="0" cellspacing="0" cellpadding="10">
        <tr>
            <td valign="top">&nbsp;<br>

                <% write_column(1); %>

                <div class="panel">
                    <a href="javascript:show_select_report_page(1)">[add report to dashboard column 1]</a>
                </div>

                <td valign="top">&nbsp;<br>

                    <% write_column(2); %>

                    <div class="panel">
                        <a href="javascript:show_select_report_page(2)">[add report to dashboard column 2]</a>
                    </div>
    </table>
    <form id="addform" method="get" action="update_dashboard.aspx">
        <input type="hidden" name="rp_id">
        <input type="hidden" name="rp_chart_type">
        <input type="hidden" name="rp_col">
        <input type="hidden" name="actn" value="add">
        <input type="hidden" name="ses" value="<% Response.Write(ses); %>">
    </form>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
    <% Response.Write(Application["custom_footer"]); %>
</asp:Content>

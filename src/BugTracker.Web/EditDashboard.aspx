<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="EditDashboard.aspx.cs" Inherits="BugTracker.Web.EditDashboard" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Register TagPrefix="BugTracker" TagName="MainMenu" Src="~/Core/Controls/MainMenu.ascx" %>

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

        function show_select_report_page(whichCol) {

            col = whichCol;
            popup_window = window.open('SelectReport.aspx');
        }

        function add_selected_report(chartType, id) {
            var frm = document.getElementById("addform");
            frm.rp_chart_type.value = chartType;
            frm.rp_id.value = id;
            frm.rp_col.value = col;
            frm.submit();
        }
    </script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
    <BugTracker:MainMenu runat="server" ID="MainMenu"/>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <a href="Dashboard.aspx">back to dashboard</a>
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
    <form id="addform" method="get" action="UpdateDashboard.aspx">
        <input type="hidden" name="rp_id">
        <input type="hidden" name="rp_chart_type">
        <input type="hidden" name="rp_col">
        <input type="hidden" name="actn" value="add">
        <input type="hidden" name="ses" value="<% Response.Write(this.Ses); %>">
    </form>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
    <% Response.Write(Application["custom_footer"]); %>
</asp:Content>

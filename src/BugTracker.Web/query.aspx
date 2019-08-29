<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="query.aspx.cs" Inherits="BugTracker.Web.query" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <script type="text/javascript" src="sortable.js"></script>

    <script>
        var shown = true;

        function showhide_form() {
            var frm = document.getElementById("<% Response.Write(Util.get_form_name()); %>");
            if (shown) {
                frm.style.display = "none";
                shown = false;
                showhide.firstChild.nodeValue = "show form";
            } else {
                frm.style.display = "block";
                shown = true;
                showhide.firstChild.nodeValue = "hide form";
            }
        }

        function on_dbtables_changed() {
            var tables_sel = document.getElementById("dbtables_select");
            selected_text = tables_sel.options[tables_sel.selectedIndex].text;
            if (selected_text != "Select Table") {
                document.getElementById("queryText").value = "select * from " + selected_text;
            }
        }
    </script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div class="align">
        <table border="0">

            <tr>

                <td style="background: yellow; border: red solid 2px; color: #ff0000; font-size: 8pt; font-weight: bold; padding: 4px;">
                This page is not safe on a public web server.
            After you install BugTracker.NET on a public web server, please delete it.

            <tr>
                <td>
                    <select id="dbtables_select" runat="server" onchange="on_dbtables_changed()" />
                    <div style="float: right;">
                        <a href="javascript:showhide_form()" id="showhide">hide form</a>
                    </span>

            <tr>
                <td>

                    <form class="frm" action="query.aspx" method="POST" runat="server">
                        Or enter SQL:
                <br>
                        <textarea rows="15" cols="70" runat="server" id="queryText"></textarea>
                        <p>
                            <input runat="server" type="submit" value="Execute SQL" />
                    </form>

        </table>
    </div>

    <%

        if (this.exception_message != "")
            Response.Write("<span class=err>" + this.exception_message + "</span><br>");

        if (this.ds != null && this.ds.Tables.Count > 0 && this.ds.Tables[0].Rows.Count > 0)
            SortableHtmlTable.create_from_dataset(
                Response, this.ds, "", "");
        else
            Response.Write("No Rows");
    %>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
</asp:Content>

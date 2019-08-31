<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" ValidateRequest="false" AutoEventWireup="true" CodeBehind="EditCustomHtml.aspx.cs" Inherits="BugTracker.Web.Administration.EditCustomHtml" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <script type="text/javascript" src="<%= ResolveUrl("~/Scripts/edit_area/edit_area_full.js")%>"></script>

    <script>
        editAreaLoader.init({
            id: "myedit" // id of the textarea to transform
            ,
            start_highlight: true // if start with highlight
            ,
            toolbar: "search, go_to_line, undo, redo, help",
            browsers: "all",
            language: "en",
            syntax: "sql",
            allow_toggle: false,
            min_width: 800,
            min_height: 400
        });

        function load_custom_file() {
            var sel = document.getElementById("which");
            window.location = "<%= ResolveUrl("~/Administration/EditCustomHtml.aspx?which=")%>" + sel.options[sel.selectedIndex].value;
        }
    </script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
    <% this.Security.WriteMenu(Response, "admin"); %>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div class="align">
        <table border="0" style="margin-left: 20px; margin-top: 20px; width: 80%;">
            <tr>
                <td>
                    <form runat="server">
                        Select custom html file:
                <select id="which" onchange="load_custom_file()" runat="server">
                    <option value="css">btnet_custom.css</option>
                    <option value="footer">customer_footer.html</option>
                    <option value="header">customer_header.html</option>
                    <option value="logo">customer_logo.html</option>
                    <option value="welcome">customer_welcome.html</option>
                </select>

                        <p>

                            <textarea id="myedit" runat="server" style="width: 100%"></textarea>
                        <p>

                            <div class="err" id="msg" runat="server">&nbsp;</div>

                            <div>
                                <input type="submit" value="Save" class="btn">
                            </div>

                    </form>
        </table>
    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
    <% Response.Write(Application["custom_footer"]); %>
</asp:Content>

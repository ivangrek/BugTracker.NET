<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="massedit.aspx.cs" Inherits="BugTracker.Web.massedit" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
    <% this.security.write_menu(Response, "admin"); %>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div class="align">
        <p>
            <div runat="server" id="msg" class="err">&nbsp;</div>

            <p>
                <a href="search.aspx">back to search</a>

        <p>
            or
        <p>
            <script>
                function submit_form() {
                    var frm = document.getElementById("frm");
                    frm.submit();
                    return true;
                }

            </script>
            <form runat="server" id="frm">
                <a style="border: 1px red solid; padding: 3px;" id="confirm_href" runat="server" href="javascript: submit_form()"></a>
                <input type="hidden" id="bug_list" runat="server" />
                <input type="hidden" id="update_or_delete" runat="server" />
            </form>


            <p>
                &nbsp;
        <p>
        <p>
            <div class="err">Email notifications are not sent when updates are made via this page.</div>
            <p>
                This SQL statement will execute when you confirm:
        <pre id="sql_text" runat="server"></pre>

    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
    <% Response.Write(Application["custom_footer"]); %>
</asp:Content>

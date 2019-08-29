<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="svn_log.aspx.cs" Inherits="BugTracker.Web.svn_log" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <script type="text/javascript" src="version_control_sel_rev.js"></script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <form id="frm" target="_blank" action="svn_diff.aspx" method="GET">

        <input type="hidden" name="rev_0" id="rev_0" value="0" />
        <input type="hidden" name="rev_1" id="rev_1" value="0" />
        <input type="hidden" name="path_0" id="path_0" value="" />
        <input type="hidden" name="path_1" id="path_1" value="" />
        <input type="hidden" name="revpathid" id="revpathid" value="" runat="server" />

    </form>

    <p>

        <table border="1" class="datat">
            <tr>
                <td class="datah">
                revision
        <td class="datah">
                author
        <td class="datah">
                date
        <td class="datah">
                path
        <td class="datah">action<br>
            <td class="datah">
                msg
        <td class="datah">
                view
        <td class="datah">view<br>
            annotated<br>
            (svn blame)
        <td class="datah">
            <span></span>
            <a
                style="background: yellow; border-bottom: 2px black solid; border-left: 1px silver solid; border-right: 2px black solid; border-top: 1px silver solid; display: none;"
                id="do_diff_enabled" href="javascript:on_do_diff()">click<br>
                to<br>
                diff
            </a>
            <a style="color: red;" id="do_diff_disabled" href="javascript:on_do_diff()">select<br>
                two<br>
                revisions</a></span>
        <% fetch_and_write_history(this.file_path); %>
        </table>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
</asp:Content>

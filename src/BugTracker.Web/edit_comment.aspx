<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" ValidateRequest="false" AutoEventWireup="true" CodeBehind="edit_comment.aspx.cs" Inherits="BugTracker.Web.edit_comment" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <script type="text/javascript" language="JavaScript" src="jquery/jquery-1.3.2.min.js"></script>
    <script type="text/javascript" language="JavaScript" src="jquery/jquery-ui-1.7.2.custom.min.js"></script>
    <script type="text/javascript" language="JavaScript" src="jquery/jquery.textarearesizer.compressed.js"></script>
    <% if (this.security.user.use_fckeditor)
        { %>
    <script type="text/javascript" src="ckeditor/ckeditor.js"></script>
    <% } %>

    <script>

        $(document).ready(do_doc_ready);

        function do_doc_ready() {

            <%

        if (this.use_fckeditor)
            Response.Write("CKEDITOR.replace( 'comment' )");
        else
            Response.Write("$('textarea.resizable:not(.processed)').TextAreaResizer()");
            %>

        }
    </script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
    <% this.security.write_menu(Response, Util.get_setting("PluralBugLabel", "bugs")); %>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div class="align">
        <table border="0">
            <tr>
                <td>

                    <a href="edit_bug.aspx?id=" <% Response.Write(Convert.ToString(this.bugid)); %>>back to <% Response.Write(Util.get_setting("SingularBugLabel", "bug")); %></a>
                    <form class="frm" runat="server">

                        <table border="0">
                            <tr>
                                <td colspan="3">
                                    <textarea rows="16" cols="80" runat="server" class="txt resizable" id="comment"></textarea>
                                <tr>
                                    <td colspan="3">
                                        <asp:CheckBox runat="server" class="cb" ID="internal_only" />
                                        <span runat="server" id="internal_only_label">Visible to internal users only</span>
                                    </td>
                                </tr>

                            <tr>
                                <td colspan="3" align="left">
                                    <span runat="server" class="err" id="msg">&nbsp;</span>
                                <tr>
                                    <td colspan="2" align="center">
                                        <input runat="server" class="btn" type="submit" id="sub" value="Update" />
                        </table>
                    </form>
                </td>
            </tr>
        </table>
    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
    <% Response.Write(Application["custom_footer"]); %>
</asp:Content>

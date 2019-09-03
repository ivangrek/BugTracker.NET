<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Edit.aspx.cs" Inherits="BugTracker.Web.Attachments.Edit" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
    <% this.Security.WriteMenu(Response, Util.GetSetting("PluralBugLabel", "bugs")); %>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div class="align">
        <table border="0">
            <tr>
                <td>
                    <a href="EditBug.aspx?id=" <% Response.Write(Convert.ToString(this.Bugid)); %>>back to <% Response.Write(Util.GetSetting("SingularBugLabel", "bug")); %></a>
                    <form class="frm" runat="server">
                        <table border="0">

                            <tr>
                                <td class="lbl">Description:</td>
                                <td>
                                    <input runat="server" type="text" class="txt" id="desc" maxlength="80" size="80">
                                </td>
                                <td runat="server" class="err" id="desc_err">&nbsp;</td>
                            </tr>

                            <tr>
                                <td class="lbl">Filename:</td>
                                <td>
                                    <b>
                                        <span id="filename" runat="server">&nbsp;</span>
                                    </b>
                                </td>
                                <td>&nbsp;</td>
                            </tr>


                            <tr>
                                <td colspan="3">
                                    <asp:CheckBox runat="server" class="cb" ID="internal_only" />
                                    <span runat="server" id="internal_only_label">Visible to internal users only</span>
                                </td>
                            </tr>


                            <tr>
                                <td colspan="3" align="left">
                                    <span runat="server" class="err" id="msg">&nbsp;</span>
                                </td>
                            </tr>

                            <tr>
                                <td colspan="2" align="center">
                                    <input runat="server" class="btn" type="submit" id="sub" value="Update" />
                                    <td>&nbsp</td>
                                </td>
                            </tr>
                </td>
            </tr>
        </table>
        </form>
        </td></tr></table>
    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
    <% Response.Write(Application["custom_footer"]); %>
</asp:Content>

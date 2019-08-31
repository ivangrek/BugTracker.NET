<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Edit.aspx.cs" Inherits="BugTracker.Web.Administration.Statuses.Edit" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
    <% this.Security.WriteMenu(Response, "admin"); %>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div class="align">
        <a href="<%= ResolveUrl("~/Administration/Statuses/List.aspx")%>">back to statuses</a>
        <div>&nbsp;</div>

        <table style="border-collapse: collapse; border: 0;">
            <tr>
                <td>
                    
                    <form class="frm" runat="server">
                        <table border="0">
                            <tr>
                                <td class="lbl">Description:</td>
                                <td>
                                    <input runat="server" type="text" class="txt" id="name" maxlength="20" size="20" />
                                </td>
                                <td runat="server" class="err" id="nameErr">&nbsp;</td>
                            </tr>

                            <tr>
                                <td colspan="3">
                                    <span class="smallnote">Sort Sequence controls the sort order in the dropdowns.</span>
                                </td>
                            </tr>

                            <tr>
                                <td class="lbl">Sort Sequence:</td>
                                <td>
                                    <input runat="server" type="text" class="txt" id="sortSeq" maxlength="2" size="2" />
                                </td>
                                <td runat="server" class="err" id="sortSeqErr">&nbsp;</td>
                            </tr>

                            <tr>
                                <td colspan="3">
                                    <span class="smallnote">CSS Class can be used to control the look of lists.<br>
                                        See the example queries.</span>
                                </td>
                            </tr>

                            <tr>
                                <td class="lbl">CSS Class:</td>
                                <td>
                                    <input runat="server" type="text" class="txt" id="style" value="" maxlength="10" size="10" />
                                    &nbsp;&nbsp;<a target="_blank" href="<%= ResolveUrl("~/Administration/EditStyles.aspx")%>">more CSS info...</a>
                                </td>
                                <td runat="server" class="err" id="style_err">&nbsp;</td>
                            </tr>

                            <tr>
                                <td class="lbl">Default Selection:</td>
                                <td>
                                    <asp:CheckBox runat="server" class="cb" ID="defaultSelection" />
                                </td>
                                <td>&nbsp;</td>
                            </tr>

                            <tr>
                                <td colspan="3" align="left">
                                    <span runat="server" class="err" id="msg">&nbsp;</span>
                                </td>
                            </tr>

                            <tr>
                                <td colspan="2" align="center">
                                    <input runat="server" class="btn" type="submit" id="sub" value="Create or Edit" />
                                </td>
                                <td>&nbsp;</td>
                            </tr>
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

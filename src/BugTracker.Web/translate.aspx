<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" ValidateRequest="false" AutoEventWireup="true" CodeBehind="Translate.aspx.cs" Inherits="BugTracker.Web.Translate" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Register TagPrefix="BugTracker" TagName="MainMenu" Src="~/Core/Controls/MainMenu.ascx" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
    <BugTracker:MainMenu runat="server" ID="MainMenu"/>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div class="align">
        <table border="0">
            <tbody>
                <tr>
                    <td>
                        <a id="back_href" href="" runat="server">back to <% Response.Write(Util.GetSetting("SingularBugLabel", "bug")); %></a>
                        <form enctype="multipart/form-data" runat="server">
                            <table border="0" class="frm">
                                <tbody>
                                    <tr>
                                        <td class="lbl">Translation mode:
                                            <asp:DropDownList ID="mode" runat="server" />
                                        </td>
                                    </tr>
                                    <td class="lbl">Source text:
                                    </td>
                </tr>
                <tr>
                    <td>
                        <textarea class="txt" id="source" rows="15" cols="72" runat="server"></textarea>
                    </td>
                </tr>
                <tr>
                    <td align="middle">
                        <input class="btn" id="sub" type="submit" value="Translate" runat="server" />
                    </td>
                </tr>
            </tbody>
        </table>
        <br>
        <asp:Panel ID="pnl" runat="server" Visible="false">
            <table class="cmt">
                <tbody>
                    <tr>
                        <td>
                            <span class="pst">translated from <%= this.mode.SelectedItem %> on <%= DateTime.Now %></span>
                            <br>
                            <br>
                            <asp:Label ID="dest" runat="server" CssClass="cmt_text" />
                        </td>
                    </tr>
                </tbody>
            </table>
        </asp:Panel>
        <input id="bugid" type="hidden" runat="server" />
        </form></td>
        </tr>
        </tbody>
        </table>

    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
</asp:Content>

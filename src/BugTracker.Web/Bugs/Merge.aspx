<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Merge.aspx.cs" Inherits="BugTracker.Web.Bugs.Merge" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Register TagPrefix="BugTracker" TagName="MainMenu" Src="~/Core/Controls/MainMenu.ascx" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
    <BugTracker:MainMenu runat="server" ID="MainMenu"/>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <p>
        <div class="align">
            <table border="0">
                <tr>
                    <td>
                        <a id="back_href" runat="server" href="">back to <% Response.Write(ApplicationSettings.SingularBugLabel); %></a>
                        <!--<a id="confirm_href" runat="server" href="">confirm delete</a>
                </a>-->
                        <p>
                            Merge all comments, attachments, and subscriptions
                from "FROM" <% Response.Write(ApplicationSettings.SingularBugLabel); %>
                into "INTO" <% Response.Write(ApplicationSettings.SingularBugLabel); %>.
                <br>
                            <span class="err">Note:&nbsp;&nbsp;"FROM" <% Response.Write(ApplicationSettings.SingularBugLabel); %>
                        will be deleted!</err>
                <p>

                    <form runat="server" class="frm">
                        <table border="0">

                            <tr>
                                <td class="lbl" align="right">FROM <% Response.Write(ApplicationSettings.SingularBugLabel); %>
                                :
                        <td align="left" valign="bottom">
                        <input type="text" class="txt" id="from_bug" runat="server" size="8"/>
                            <span class="stat" id="static_from_bug" runat="server" style="display: none;"></span>
                            <br>
                            <span class="stat" id="static_from_desc" runat="server" style="display: none;"></span>

                            <tr>
                                <td colspan="2"><span class="err" id="from_err" runat="server">&nbsp;</span>
                                <tr>
                                    <td class="lbl" align="right">INTO <% Response.Write(ApplicationSettings.SingularBugLabel); %>
                                    :
                        <td align="left" valign="bottom">
                        <input type="text" class="txt" id="into_bug" runat="server" size="8"/>
                            <span class="stat" id="static_into_bug" runat="server" style="display: none;"></span>
                            <br>
                            <span class="stat" id="static_into_desc" runat="server" style="display: none;"></span>

                            <tr>
                                <td colspan="2"><span class="err" id="into_err" runat="server">&nbsp;</span>
                            </tr>


                            <tr>
                                <td colspan="2" align="center">
                                    <br>
                                <input class="btn" type="submit" runat="server" id="submit" value="Merge"/>
                        </table>

                        <input type="hidden" id="confirm" runat="server"/>
                        <input type="hidden" id="prev_from_bug" runat="server"/>
                        <input type="hidden" id="prev_into_bug" runat="server"/>
                        <input type="hidden" id="orig_id" runat="server"/>
                    </form>

                    <p>
                    </td>
                </tr>
            </table>
        </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
    <% Response.Write(Application["custom_footer"]); %>
</asp:Content>

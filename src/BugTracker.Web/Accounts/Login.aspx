<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" ValidateRequest="false" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="BugTracker.Web.Accounts.Home" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
    <div style="float: right;">
        <span>
            <a target="_blank" style="font-family: arial; font-size: 7pt; letter-spacing: 1px;" href="http://ifdefined.com/bugtrackernet.html">BugTracker.NET</a>
            <br>
            <a target="_blank" style="font-family: arial; font-size: 7pt; letter-spacing: 1px;" href="http://ifdefined.com/README.html">Help</a>
            <br>
            <a target="_blank" style="font-family: arial; font-size: 7pt; letter-spacing: 1px;" href="mailto:ctrager@yahoo.com">Feedback</a>
            <br>
            <a target="_blank" style="font-family: arial; font-size: 7pt; letter-spacing: 1px;" href="<%= ResolveUrl("~/Content/about.html") %>">About</a>
            <br>
            <a target="_blank" style="font-family: arial; font-size: 7pt; letter-spacing: 1px;" href="http://ifdefined.com/README.html">Donate</a>
        </span>
    </div>

    <table style="border-spacing: 0;">
        <tr>
            <% Response.Write(Application["custom_logo"]); %>
        </tr>
    </table>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div>
        <table style="border-spacing: 0; margin: 0 auto;">
            <tr>
                <td>
                    <form class="frm" runat="server">
                        <table style="border-spacing: 0;">
                            <% if (Util.GetSetting("WindowsAuthentication", "0") != "0")
                                { %>
                            <tr>
                                <td colspan="2" class="smallnote">To login using your Windows ID, leave User blank</td>
                            </tr>
                            <% } %>
                            <tr>
                                <td class="lbl">User:</td>
                                <td>
                                    <input runat="server" type="text" class="txt" id="user"/>
                                </td>
                            </tr>

                            <tr>
                                <td class="lbl">Password:</td>
                                <td>
                                    <input runat="server" type="password" class="txt" id="pw"/>
                                </td>
                            </tr>

                            <tr>
                                <td colspan="2" style="text-align: left;">
                                    <span runat="server" class="err" id="msg">&nbsp;</span>
                                </td>
                            </tr>

                            <tr>
                                <td colspan="2" style="text-align: center;">
                                    <input class="btn" type="submit" value="Logon" runat="server"/>
                                </td>
                            </tr>

                        </table>
                    </form>

                    <span>

                        <% if (Util.GetSetting("AllowGuestWithoutLogin", "0") == "1")
                            { %>
                        <p>
                            <a style="font-size: 8pt;" href="<%= ResolveUrl("~/Bugs/List.aspx") %>">Continue as "guest" without logging in</a>
                        <p>
                            <% } %>

                            <% if (Util.GetSetting("AllowSelfRegistration", "0") == "1")
                                { %>
                        <p>
                            <a style="font-size: 8pt;" href="<%= ResolveUrl("~/Accounts/Register.aspx") %>">Register</a>
                        <p>
                            <% } %>

                            <% if (Util.GetSetting("ShowForgotPasswordLink", "1") == "1")
                                { %>
                        <p>
                            <a style="font-size: 8pt;" href="<%= ResolveUrl("~/Accounts/Forgot.aspx") %>">Forgot your username or password?</a>
                        <p>
                            <% } %>
                    </span>
                </td>
            </tr>
        </table>

        <% Response.Write(Application["custom_welcome"]); %>
    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
</asp:Content>

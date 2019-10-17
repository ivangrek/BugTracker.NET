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
    <div style="width: 300px; margin: 0 auto;">
        <form class="frm" runat="server">
            <% if (ApplicationSettings.WindowsAuthentication != 0)
                { %>
            <div style="margin-bottom: 10px;">
                <span class="smallnote">To login using your Windows ID, leave User blank</span>
            </div>
            <% } %>
            <div style="margin-bottom: 10px;">
                <div class="lbl" style="margin-bottom: 5px;">User:</div>
                <div>
                    <input runat="server" type="text" class="txt" id="user" style="box-sizing: border-box; width: 100%;" />
                </div>
            </div>

            <div style="margin-bottom: 10px;">
                <div class="lbl" style="margin-bottom: 5px;">Password:</div>
                <div>
                    <input runat="server" type="password" class="txt" id="pw" style="box-sizing: border-box; width: 100%;" />
                </div>
            </div>

            <div style="text-align: left;">
                <span runat="server" class="err" id="msg">&nbsp;</span>
            </div>

            <div style="text-align: right;">
                <input class="btn" type="submit" value="Logon" runat="server" />
            </div>
        </form>

        <div>
            <% if (ApplicationSettings.AllowGuestWithoutLogin)
                { %>
            <p>
                <a style="font-size: 8pt;" href="<%= ResolveUrl("~/Bug") %>">Continue as "guest" without logging in</a>
            </p>
            <% } %>

            <% if (ApplicationSettings.AllowSelfRegistration)
                { %>
            <p>
                <a style="font-size: 8pt;" href="<%= ResolveUrl("~/Account/Registe") %>">Register</a>
            </p>
            <% } %>

            <% if (ApplicationSettings.ShowForgotPasswordLink)
                { %>
            <p>
                <a style="font-size: 8pt;" href="<%= ResolveUrl("~/Account/Forgot") %>">Forgot your username or password?</a>
            </p>
            <% } %>
        </div>

        <% Response.Write(Application["custom_welcome"]); %>
    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
</asp:Content>

<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Forgot.aspx.cs" Inherits="BugTracker.Web.Accounts.Forgot" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <%--TODO <body onload="document.forms[0].email.focus()">--%>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <table border="0">
        <tr>

            <%

                Response.Write(Application["custom_logo"]);
            %>
    </table>


    <div align="center">
        <table border="0">
            <tr>
                <td>

                    <form class="frm" runat="server">
                        <table border="0">

                            <tr>
                                <td colspan="2" class="smallnote">Enter Username or Email or both</td>
                            </tr>

                            <tr>
                                <td colspan="2">&nbsp;</td>
                            </tr>

                            <tr>
                                <td class="lbl">Username:</td>
                                <td>
                                    <input runat="server" type="text" class="txt" id="username" size="40" maxlength="40"/>
                                </td>
                            </tr>

                            <tr>
                                <td class="lbl">Email:</td>
                                <td>
                                    <input runat="server" type="text" class="txt" id="email" size="40" maxlength="40"/>
                                </td>
                            </tr>

                            <tr>
                                <td colspan="2" align="left">
                                    <span runat="server" class="err" id="msg">&nbsp;</span>
                                </td>
                            </tr>

                            <tr>
                                <td colspan="2" align="center">
                                    <input class="btn" type="submit" value="Send password info to my email" runat="server"/>
                                </td>
                            </tr>

                        </table>
                    </form>

                    <a href="<%= ResolveUrl("~/Accounts/Login.aspx") %>">Return to login page</a>

                </td>
            </tr>
        </table>

    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
</asp:Content>

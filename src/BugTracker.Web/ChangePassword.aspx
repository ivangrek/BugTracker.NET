<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" ValidateRequest="false" AutoEventWireup="true" CodeBehind="ChangePassword.aspx.cs" Inherits="BugTracker.Web.ChangePassword" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <%--TODO <body onload="document.forms[0].password.focus()">--%>
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
                                <td class="lbl">New Password:</td>
                                <td>
                                    <input runat="server" type="password" class="txt" id="password" size="20" maxlength="20" autocomplete="off">
                                </td>
                            </tr>

                            <tr>
                                <td class="lbl">Reenter Password:</td>
                                <td>
                                    <input runat="server" type="password" class="txt" id="confirm" size="20" maxlength="20" autocomplete="off">
                                </td>
                            </tr>

                            <tr>
                                <td colspan="2" align="left">
                                    <span runat="server" class="err" id="msg">&nbsp;</span>
                                </td>
                            </tr>

                            <tr>
                                <td colspan="2" align="center">
                                    <input class="btn" type="submit" value="Change password" runat="server" />
                                </td>
                            </tr>

                        </table>
                    </form>

                    <a href="Home.aspx">Go to login page</a>

                </td>
            </tr>
        </table>

    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
</asp:Content>

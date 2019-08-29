<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="complete_registration.aspx.cs" Inherits="BugTracker.Web.complete_registration" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
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

                    <div runat="server" class="err" id="msg">&nbsp;</div>
                    <p>
                        <a href="default.aspx">Go to login page</a>
                </td>
            </tr>
        </table>

    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
</asp:Content>

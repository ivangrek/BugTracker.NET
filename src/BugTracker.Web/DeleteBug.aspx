<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="DeleteBug.aspx.cs" Inherits="BugTracker.Web.DeleteBug" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
    <% this.Security.WriteMenu(Response, "admin"); %>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <p>
        <div class="align">
            <p>&nbsp</p>
            <a id="back_href" runat="server" href="">back to <% Response.Write(Util.GetSetting("SingularBugLabel", "bug")); %></a>

            <p>
                or
        <p>

            <script>
                function submit_form() {
                    var frm = document.getElementById("frm");
                    frm.submit();
                    return true;
                }

            </script>
            <form runat="server" id="frm">
                <a id="confirm_href" runat="server" href="javascript: submit_form()"></a>
                <input type="hidden" id="row_id" runat="server">
            </form>


        </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
    <% Response.Write(Application["custom_footer"]); %>
</asp:Content>

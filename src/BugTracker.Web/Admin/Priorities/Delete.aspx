<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Delete.aspx.cs" Inherits="BugTracker.Web.Administration.Priorities.Delete" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Register TagPrefix="BugTracker" TagName="MainMenu" Src="~/Core/Controls/MainMenu.ascx" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <script>
        function submit_form() {
            var frm = document.getElementById("frm");
            frm.submit();
            return true;
        }
    </script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
    <BugTracker:MainMenu runat="server" ID="MainMenu"/>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div class="main">
        <a href="<%= ResolveUrl("~/Admin/Priorities/List.aspx")%>">back to priorities</a>

        <p>or</p>

        <form runat="server" id="frm">
            <a id="confirmHref" runat="server" href="javascript: submit_form()"></a>
            <input type="hidden" id="rowId" runat="server" />
        </form>
    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
    <% Response.Write(Application["custom_footer"]); %>
</asp:Content>

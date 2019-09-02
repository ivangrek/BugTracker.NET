<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Delete.aspx.cs" Inherits="BugTracker.Web.Administration.Categories.Delete" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>

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
    <% this.Security.WriteMenu(Response, "admin"); %>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div class="align">
        <a href="<%= ResolveUrl("~/Administration/Categories/List.aspx")%>">back to categories</a>

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

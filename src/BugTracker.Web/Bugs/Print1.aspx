<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Print1.aspx.cs" Inherits="BugTracker.Web.Bugs.Print1" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Register TagPrefix="BugTracker" TagName="MainMenu" Src="~/Core/Controls/MainMenu.ascx" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <style>
        a {
            text-decoration: underline;
        }

            a:visited {
                text-decoration: underline;
            }

            a:hover {
                text-decoration: underline;
            }
    </style>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div runat="server" id="mainBlock">
        <%
            PrintBug.print_bug(Response, this.Dr, Security,
                false, // include style
                this.ImagesInline, this.HistoryInline,
                true); // internal_posts 
        %>
    </div>

    <div runat="server" id="errorBlock" class="align" Visible="False">
        <BugTracker:MainMenu runat="server" ID="MainMenu" />

        <div class="align">
            <div class="err">
                <%= Util.CapitalizeFirstLetter(Util.GetSetting("SingularBugLabel", "bug")) %>
                not found:&nbsp;<%= Convert.ToString(this.Id)%>
            </div>

            <p></p>

            <a href='<%= ResolveUrl("~/Bugs/List.aspx")%>'>View
                <%= Util.GetSetting("PluralBugLabel", "bug")%>
            </a>
        </div>
    </div>
    
    <div runat="server" id="errorBlockPermissions" class="align" Visible="False">
        <div class="err">
            You are not allowed to view this <%= Util.GetSetting("SingularBugLabel", "bug") %>
            not found:&nbsp;<%= Convert.ToString(this.Id)%>
        </div>

        <p></p>

        <a href='<%= ResolveUrl("~/Bugs/List.aspx")%>'>View
            <%= Util.CapitalizeFirstLetter(Util.GetSetting("PluralBugLabel", "bug")) %>
        </a>
    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
</asp:Content>

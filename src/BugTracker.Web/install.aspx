<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Install.aspx.cs" Inherits="BugTracker.Web.Install" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <p>
        <b>How to get BugTracker.NET up and running</b>
    <p>
        1) Create a SQL Server database. You can use the form below.
    <p>
        2) Update the database connection string in Web.config to point to your database.
    <p>
        3) Copy/paste the text in the file <a target="_blank" href="setup.sql">"setup.sql"</a> into <a target="_blank" href="Query.aspx">this form</a>
    and run it.
    <p>
        4) Logon at <a href="default.aspx">default.aspx</a>
    using user "admin" and password "admin"
    <p>
        You probably should spend time looking at the README.HTML and Web.config files. If you have any questions, post them to the <a href="http://sourceforge.net/projects/btnet/forums/forum/226938">Help Forum</a>.

    <hr>
        <form action="Install.aspx" method="GET">
            Database Name:&nbsp;<input name="dbname">
            <br>
            <input type="submit" value="Create Database">
        </form>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
</asp:Content>

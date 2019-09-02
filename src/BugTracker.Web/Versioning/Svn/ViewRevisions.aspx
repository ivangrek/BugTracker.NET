<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ViewRevisions.aspx.cs" Inherits="BugTracker.Web.Versioning.Svn.ViewRevisions" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <%--TODO <body width="600">--%>
    <script type="text/javascript" src="<%= ResolveUrl("~/Scripts/sortable.js") %>"></script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div class="align">
        SVN File Revisions for <% Response.Write(Util.GetSetting("SingularBugLabel", "bug")); %>&nbsp;<% Response.Write(Convert.ToString(this.Bugid)); %>
        <p>
            <%
                if (this.Ds.Tables[0].Rows.Count > 0)
                    SortableHtmlTable.CreateFromDataSet(
                        Response, this.Ds, "", "", false);
                else
                    Response.Write("No revisions.");
            %>
    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
</asp:Content>

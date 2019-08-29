<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="hg_view_revisions.aspx.cs" Inherits="BugTracker.Web.hg_view_revisions" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <%--TODO <body width="600">--%>
    <script type="text/javascript" language="JavaScript" src="sortable.js"></script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div class="align">
        hg File revisions for <% Response.Write(Util.get_setting("SingularBugLabel", "bug")); %>&nbsp;<% Response.Write(Convert.ToString(this.bugid)); %>
        <p>
            <%
                if (this.ds.Tables[0].Rows.Count > 0)
                    SortableHtmlTable.create_from_dataset(
                        Response, this.ds, "", "", false);
                else
                    Response.Write("No revisions.");
            %>
    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
</asp:Content>

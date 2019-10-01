<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ViewSubscribers.aspx.cs" Inherits="BugTracker.Web.ViewSubscribers" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <%--TODO
    <body width="600">--%>
    <script type="text/javascript" src="<%= ResolveUrl("~/Scripts/sortable.js") %>"></script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div class="align">
        Subscribers for <% Response.Write(Convert.ToString(this.Bugid)); %>
        <p>

            <table border="0">
                <tr>
                    <td>
                        <form class="frm" runat="server" action="~/ViewSubscribers.aspx">
                            <table>
                                <tr>
                                    <td><span class="lbl">add subscriber:</span>

                                        <asp:DropDownList ID="userid" runat="server">
                                        </asp:DropDownList>
                                    <tr>
                                        <td colspan="2">
                                            <input class="btn" type="submit" value="Add">
                                        <tr>
                                            <td colspan="2">&nbsp;<span runat="server" class="err" id="add_err"></span>
                            </table>
                            <input type="hidden" name="id" value="<% Response.Write(Convert.ToString(this.Bugid)); %>">
                            <input type="hidden" name="actn" value="add">
                        </form>
                    </td>
                </tr>
            </table>

            <%
                if (this.Ds.Tables[0].Rows.Count > 0)
                    SortableHtmlTable.CreateFromDataSet(
                        Response, this.Ds, "", "", false);
                else
                    Response.Write("No subscribers for this bug.");
            %>
    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
</asp:Content>

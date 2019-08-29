<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="view_subscribers.aspx.cs" Inherits="BugTracker.Web.view_subscribers" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <%--TODO
    <body width="600">--%>
    <script type="text/javascript" src="sortable.js"></script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div class="align">
        Subscribers for <% Response.Write(Convert.ToString(this.bugid)); %>
        <p>

            <table border="0">
                <tr>
                    <td>
                        <form class="frm" runat="server" action="view_subscribers.aspx">
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
                            <input type="hidden" name="id" value="<% Response.Write(Convert.ToString(this.bugid)); %>">
                            <input type="hidden" name="actn" value="add">
                        </form>
                    </td>
                </tr>
            </table>

            <%
                if (this.ds.Tables[0].Rows.Count > 0)
                    SortableHtmlTable.create_from_dataset(
                        Response, this.ds, "", "", false);
                else
                    Response.Write("No subscribers for this bug.");
            %>
    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
</asp:Content>

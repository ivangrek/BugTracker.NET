<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="relationships.aspx.cs" Inherits="BugTracker.Web.relationships" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <%--TODO <body onload="body_on_load()">--%>

    <script type="text/javascript" language="JavaScript" src="sortable.js"></script>

    <script>
        var asp_form_id = '<% Response.Write(Util.get_form_name()); %>';

        function remove(bugid2_arg) {
            var frm = document.getElementById(asp_form_id);
            var actn = document.getElementById("actn");
            actn.value = "remove";
            document.getElementById("bugid2").value = bugid2_arg;
            frm.submit();
        }

        function body_on_load() {

            opener.set_relationship_cnt(
                <%
        Response.Write(Convert.ToString(this.bugid));
        Response.Write(",");
        Response.Write(Convert.ToString(this.ds.Tables[0].Rows.Count));
                %>
            );
        }
    </script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div class="align">
        Relationships for
    <%
        Response.Write(Util.get_setting("SingularBugLabel", "bug")
                       + " "
                       + Convert.ToString(this.bugid));
    %>

        <p>
            <table border="0">
                <tr>
                    <td>

                        <%
                            if (this.permission_level != Security.PERMISSION_READONLY)
                            {
                        %>
                        <p>
                            <form class="frm" runat="server" action="relationships.aspx">
                                <table>
                                    <tr>
                                        <td>
                                        Related ID:<td>
                                            <input type="text" class="txt" id="bugid2" name="bugid2" size="8">
                                        <tr>
                                            <td>
                                            Comment:<td>
                                                <input type="text" class="txt" id="type" name="type" size="90" maxlength="500">
                                            <tr>
                                                <td colspan="2">Related ID is sibling<asp:RadioButton runat="server" Checked="true" GroupName="direction" value="0" ID="siblings" />
                                                    &nbsp;&nbsp;&nbsp;
                            Related ID is child<asp:RadioButton runat="server" GroupName="direction" value="1" ID="child_to_parent" />
                                                    &nbsp;&nbsp;&nbsp;
                            Related ID is parent<asp:RadioButton runat="server" GroupName="direction" value="2" ID="parent_to_child" />
                                                &nbsp;&nbsp;&nbsp;
                            <tr>
                                <td colspan="2">
                                    <input class="btn" type="submit" value="Add">
                                <tr>
                                    <td colspan="2">
                                    &nbsp;
                            <tr>
                                <td colspan="2">&nbsp;<span runat="server" class="err" id="add_err"></span>
                                </table>
                                <input runat="server" id="bgid" type="hidden" name="bgid" value="">
                                <input id="actn" type="hidden" name="actn" value="add">
                                <input id="ses" type="hidden" name="ses" value="<% Response.Write(this.ses); %>">
                            </form>
                            <% } %>
                    </td>
                </tr>
            </table>

        </p>
        <%

            if (this.ds.Tables[0].Rows.Count > 0)
            {
                SortableHtmlTable.create_from_dataset(
                    Response, this.ds, "", "", false);

                display_hierarchy();
            }
            else
            {
                Response.Write("No related " + Util.get_setting("PluralBugLabel", "bugs"));
            }
        %>
    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
</asp:Content>

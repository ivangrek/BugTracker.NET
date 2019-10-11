<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="EditStyles.aspx.cs" Inherits="BugTracker.Web.Administration.EditStyles" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Register TagPrefix="BugTracker" TagName="MainMenu" Src="~/Core/Controls/MainMenu.ascx" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <script type="text/javascript" src="<%= ResolveUrl("~/Scripts/sortable.js") %>"></script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
    <BugTracker:MainMenu runat="server" ID="MainMenu"/>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div class="align">

        <div class="lbl" style="width: 600px;">
            The query "demo use of css classes" has as its first column a CSS class name that is
            composed of the priority's CSS class name concatenated with the status's CSS
            class name. The SQL looks like this:
        </div>
        <p>
            <div style="font-family: courier; font-weight: bold;">
                select <span style="color: red;">isnull(pr_style + st_style,'datad')</span>, bg_id [id], bg_short_desc [desc], .... etc
            </div>
            <p>
                <div class="lbl" style="width: 600px;">
                    Note that in the sql, where there isn't both a priority CSS class and a status CSS class
            available, the default CSS class name of "datad" is used. The following list lets you see
            how all the different priority/status combinations will look. Click on a link to edit
            a priority or a status.

                </div>

                <%

                    if (this.Ds.Tables[0].Rows.Count > 0)
                        SortableHtmlTable.CreateFromDataSet(
                            Response, this.Ds, "", "", false);
                    else
                        Response.Write("No priority/status combos in the database.");
                %>

                <div cls="lbl">Relevant lines from btnet_custom.css:</div>
                <div class="frm" style="width: 600px;" id="relevant_lines" runat="server">
                </div>

    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
    <% Response.Write(Application["custom_footer"]); %>
</asp:Content>

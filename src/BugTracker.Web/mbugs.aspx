<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="MBugs.aspx.cs" Inherits="BugTracker.Web.MBugs" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Import Namespace="System.Data" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <link rel="stylesheet" href="Scripts/jquery/jquery.mobile-1.2.0.min.css" />
    <link rel="stylesheet" href="Content/mbtnet_base.css" />
    <script src="Scripts/jquery/jquery-1.8.2.min.js"></script>
    <script src="Scripts/jquery/jquery.mobile-1.2.0.min.js"></script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div class="page" data-role="page" data-cache="never">

        <div data-role="header">
            <h1 id="my_header" runat="server">Header</h1>
        </div>
        <!-- /header -->

        <div data-role="content">

            <a id="create" class="ui-submit" data-ajax="false" href="MBug.aspx?id=0" data-role="button" data-icon="arrow-r" data-iconpos="right" runat="server">Create Something</a>

            <script>
                function submit_me() {
                    document.getElementById("frm").submit();
                }
            </script>
            <form style="margin-top: 15px;" id="frm" method="get" action="MBugs.aspx" runat="server">
                <input data-mini="true" type="checkbox" id="only_mine" name="only_mine" runat="server" onchange="submit_me()" />
                <label id="only_mine_label" for="only_mine" runat="server">Show only</label>
            </form>

            <ul data-role="listview" data-theme="d" data-divider-theme="d" data-filter="true" data-filter-placeholder="Search...">

                <%
                    foreach (DataRow dr in this.Ds.Tables[0].Rows)
                    {
                        var s = @"
<li><a data-ajax='false' href=MBug.aspx?id=$ID$>
<div class=list_desc>$DESC$</div>
<div class=list_details>
    <div class=list_left>
        <span class=list_id>#$ID$</span>
        <br>
        <span class=list_project>$PROJECT$</span>
    </div>
    <div class=list_right>
        Reported by $REPORTED_USER$<br>$ASSIGNED_USER$
        <br>
        <span class=list_status>$STATUS$</span>
    </div>
</div>
</a></li>";

                        s = s.Replace("$ID$", HttpUtility.HtmlEncode(Convert.ToString(dr["id"])));
                        s = s.Replace("$DESC$", HttpUtility.HtmlEncode(Convert.ToString(dr["desc"])));
                        s = s.Replace("$PROJECT$", HttpUtility.HtmlEncode(Convert.ToString(dr["project"])));
                        s = s.Replace("$STATUS$", HttpUtility.HtmlEncode(Convert.ToString(dr["status"])));
                        s = s.Replace("$REPORTED_USER$", HttpUtility.HtmlEncode(Convert.ToString(dr["reported_user"])));
                        var assignedUser = Convert.ToString(dr["assigned_user"]);
                        if (assignedUser != "")
                            s = s.Replace("$ASSIGNED_USER$", "Assigned to " + HttpUtility.HtmlEncode(assignedUser));
                        else s = s.Replace("$ASSIGNED_USER$", "Unassigned");

                        Response.Write(s);
                    }
                %>
            </ul>

        </div>
        <!-- /end the ul -->

    </div>
    <!-- /content -->
    </div><!-- /page -->
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
</asp:Content>

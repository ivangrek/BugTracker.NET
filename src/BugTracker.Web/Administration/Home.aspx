<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Home.aspx.cs" Inherits="BugTracker.Web.Administration.Home" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>

<%@ Import Namespace="System.Threading" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <%--TODO--%>
    <%--<body <% if (this.nag) Response.Write("onload='start_animation()'"); %>>--%>

    <script>
        var nagspan;
        var color;
        var hex_chars = "0123456789ABCDEF";

        function decimal_to_hex(dec) {
            var result =
                hex_chars.charAt(Math.floor(dec / 16)) + hex_chars.charAt(dec % 16);
            return result;
        }

        function RGB2HTML(red, green, blue) {
            var rgb = "#";
            rgb += String(decimal_to_hex(red));
            rgb += String(decimal_to_hex(green));
            rgb += String(decimal_to_hex(blue));
            return rgb;
        }

        function start_animation() {
            nagspan = document.getElementById("nagspan");
            // cc = 204, 66 = 102
            color = 1;
            timer = setInterval(timer_callback, 5);
        }

        function timer_callback() {
            color += 1;

            new_color = RGB2HTML(255, color * 2, color);

            nagspan.style.background = new_color;

            if (color == 102) // if the color is now orange
            {
                clearInterval(timer);
            }
        }
    </script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
    <% this.Security.WriteMenu(Response, "admin"); %>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <% if (this.Nag)
        { %>

    <script>
        function donate() {
            pp.submit();
        }
    </script>
    <form name="pp" id="pp" action="https://www.paypal.com/cgi-bin/webscr" method="post">
        <span id="nagspan" style="background: #ff0000; border: dotted 1px blue; padding: 5px;">Is BugTracker.NET helping you or your company? Please consider <a href="javascript:donate()">donating</a>. Thanks in advance.
        </span>&nbsp;<span class="smallnote">Change if(false) to if(true) near line 27 in Administration/Admin.aspx to remove this.</span>
        <input type="hidden" name="cmd" value="_s-xclick">
        <input type="hidden" name="encrypted" value="-----BEGIN PKCS7-----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-----END PKCS7-----
">
    </form>

    <% } %>

    <div class="align">
        <table border="0">
            <tr>
                <td>
                    <ul>
                        <p>
                            <li class="listitem"><a href="Users.aspx">Users</a>
                                <p>
                                <li class="listitem"><a href="Orgs.aspx">Organizations</a>
                                    <p>
                                    <li class="listitem"><a href="Projects.aspx">Projects</a>
                                        <p>
                                        <li class="listitem"><a href="Categories.aspx">Categories</a>
                                            <p>
                                            <li class="listitem"><a href="Priorities.aspx">Priorities</a>
                                                <p>
                                                <li class="listitem"><a href="Statuses.aspx">Statuses</a>
                                                    <p>
                                                    <li class="listitem"><a href="Udfs.aspx">User Defined Attribute</a>
                                                        &nbsp;&nbsp;<span class="smallnote">(see "ShowUserDefinedBugAttribute" and "UserDefinedBugAttributeName" in Web.config)</span>
                                                        <p>
                                                        <li class="listitem"><a href="CustomFields.aspx">Custom Fields</a>
                                                            &nbsp;&nbsp;<span class="smallnote">(add custom fields to the bug page)</span>
                                                            <p>
                                                            <li class="listitem"><a target="_blank" href="Query.aspx">Run Ad-hoc Query</a>
                                                                &nbsp;&nbsp;
                    <span style="border: solid red 1px; color: red; font-size: 9px; margin: 3px; padding: 2px;">This links to Query.aspx.&nbsp;&nbsp;Query.aspx is potentially unsafe.&nbsp;&nbsp;Delete it if you are deploying on a public web server.
                    </span>
                                                                <br>
                                                                <p>
                                                                <li class="listitem"><a href="Notifications.aspx">Queued Email Notifications</a>
                                                                    <p>
                                                                    <li class="listitem"><a href="EditCustomHtml.aspx">Edit Custom Html</a>
                                                                        <p>
                                                                        <li class="listitem"><a href="EditWebConfig.aspx">Edit Web.Config</a>
                                                                            &nbsp;&nbsp;
                    <span style="border: solid red 1px; color: red; font-size: 9px; margin: 3px; padding: 2px;">Many BugTracker.NET features are configurable by editing Web.config, but please be careful! Web.config is easy to break!
                    </span>
                                                                            <br>
                                                                            <p>
                                                                            <li class="listitem"><a href="BackupDb.aspx">Backup Database</a>
                                                                                <p>
                                                                                <li class="listitem"><a href="ManageLogs.aspx">Manage Logs</a>
                    </ul>
                </td>
            </tr>
        </table>
        <p>
            &nbsp;
    <p>
        <p>
            Server Info:
    <%
        Response.Write("<br>Path=");
        Response.Write(HttpContext.Current.Server.MapPath(null));
        Response.Write("<br>MachineName=");
        Response.Write(HttpContext.Current.Server.MachineName);
        Response.Write("<br>ScriptTimeout=");
        Response.Write(HttpContext.Current.Server.ScriptTimeout);
        Response.Write("<br>.NET Version=");
        Response.Write(Environment.Version.ToString());
        Response.Write("<br>CurrentCulture=");
        Response.Write(Thread.CurrentThread.CurrentCulture.Name);
    %>
    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
    <% Response.Write(Application["custom_footer"]); %>
</asp:Content>

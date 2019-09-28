<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" ValidateRequest="false" AutoEventWireup="true" CodeBehind="SendEmail.aspx.cs" Inherits="BugTracker.Web.SendEmail" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Register TagPrefix="BugTracker" TagName="MainMenu" Src="~/Core/Controls/MainMenu.ascx" %>

<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <%--TODO <body onload="my_on_load()">--%>

    <% if (Security.User.UseFckeditor)
        { %>
    <script type="text/javascript" src="<%= ResolveUrl("~/Scripts/ckeditor/ckeditor.js") %>"></script>
    <% } %>


    <script>
        var hidden_button;
        var addr_target;

        function show_addrs(button, targ) {
            addr_target = document.getElementById(targ);
            var addrs = document.getElementById("addrs");
            addrs.style.left = findPosX(button);
            addrs.style.top = findPosY(button);
            // hide the button
            hidden_button = button;
            hidden_button.style.display = "none";
            addrs.style.display = "block";
        }

        function hide_addrs() {
            var addrs = document.getElementById("addrs");
            addrs.style.display = "none";
            hidden_button.style.display = "";
        }

        function select_addrs(sel) {
            if (addr_target.value != "") {
                addr_target.value += ", ";
            }
            addr_target.value += sel.options[sel.selectedIndex].text;
        }

        function findPosX(obj) {
            var curleft = 0;
            if (obj.offsetParent) {
                while (obj.offsetParent) {
                    curleft += obj.offsetLeft;
                    obj = obj.offsetParent;
                }
            } else if (obj.x)
                curleft += obj.x;
            return curleft;
        }

        function findPosY(obj) {
            var curtop = 0;
            if (obj.offsetParent) {
                while (obj.offsetParent) {
                    curtop += obj.offsetTop;
                    obj = obj.offsetParent;
                }
            } else if (obj.y)
                curtop += obj.y;
            return curtop;
        }

        function include_bug_click() {
            if (document.getElementById('include_bug').checked) {
                document.getElementById('include_internal_posts').disabled = false;
                document.getElementById('include_internal_posts_label').style.color = 'black';
            } else {
                document.getElementById('include_internal_posts').disabled = true;
                document.getElementById('include_internal_posts_label').style.color = 'gray';
            }
        }

        function my_on_load() {
            <%
        if (Security.User.UseFckeditor)
            Response.Write("CKEDITOR.replace( 'body' )");
            %>

            <% if (this.EnableInternalPosts)
        { %>
            document.getElementById('include_bug').onclick = include_bug_click;
            include_bug_click();
            <% } %>
        }

    </script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
    <BugTracker:MainMenu runat="server" ID="MainMenu"/>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div class="align">
        <table border="0">
            <tr>
                <td>

                    <a id="back_href" runat="server" href="">back to <% Response.Write(ApplicationSettings.SingularBugLabel); %></a>

                    <form class="frm" runat="server" enctype="multipart/form-data">
                        <table border="0">

                            <tr>
                                <td class="lbl">To:</td>
                                <td valign="top">
                                    <textarea runat="server" class="txt" id="to" cols="80" rows="2"></textarea>
                                    <input type="button" value="addresses" onclick="show_addrs(this, 'to')" class="btn" style="float: right;">
                                </td>
                                <td runat="server" class="err" id="to_err">&nbsp;</td>
                            </tr>

                            <tr>
                                <td class="lbl">From:</td>

                                <td>
                                    <asp:DropDownList ID="from" runat="server">
                                    </asp:DropDownList>
                                </td>

                                <td runat="server" class="err" id="from_err">&nbsp;</td>
                            </tr>


                            <tr>
                                <td class="lbl">CC:</td>
                                <td valign="top">
                                    <textarea runat="server" class="txt" id="cc" cols="80" rows="2"></textarea>
                                    <input type="button" value="addresses" onclick="show_addrs(this, 'cc')" class="btn" style="float: right;">
                                </td>

                                <td runat="server" class="err" id="cc_err">&nbsp;</td>
                            </tr>

                            <tr>
                                <td class="lbl">Subject:</td>
                                <td>
                                    <input runat="server" type="text" class="txt" id="subject" maxlength="200" size="100"/>
                                </td>
                                <td runat="server" class="err" id="subject_err">&nbsp;</td>
                            </tr>

                            <tr>
                                <td class="lbl">Attachment:</td>
                                <td>
                                    <input runat="server" type="file" class="txt" id="attached_file" maxlength="255" size="100"/>
                                </td>
                                <td runat="server" class="err" id="attached_file_err">&nbsp;</td>
                            </tr>

                            <tr>
                                <td class="lbl" runat="server" id="attachments_label"></td>
                                <td>
                                    <asp:CheckBoxList ID="lstAttachments" runat="server"></asp:CheckBoxList>
                                </td>
                                <td></td>
                            </tr>

                            <tr>
                                <td class="lbl">Priority:</td>
                                <td>

                                    <asp:DropDownList ID="prior" runat="server">
                                        <asp:ListItem Value="High" Text="High" />
                                        <asp:ListItem Selected="True" Value="Normal" Text="Normal" />
                                        <asp:ListItem Value="Low" Text="Low" />
                                    </asp:DropDownList>

                                </td>
                                <td>&nbsp;</td>
                            </tr>

                            <tr>
                                <td colspan="2">
                                    <input runat="server" type="checkbox" class="txt" id="return_receipt"/>Return receipt</td>
                            </tr>

                            <tr>
                                <td colspan="2">
                                    <input runat="server" type="checkbox" class="txt" id="include_bug"/>Include print of <% Response.Write(ApplicationSettings.SingularBugLabel); %></td>
                            </tr>

                            <tr>
                                <td colspan="2">
                                    <input runat="server" type="checkbox" class="txt" id="include_internal_posts"/><span id="include_internal_posts_label" runat="server">Include comments visible to internal users only</span>
                                </td>
                            </tr>

                            <tr>
                                <td colspan="3">
                                    <textarea rows="15" cols="72" runat="server" class="txt" id="body"></textarea>
                                </td>
                            </tr>

                            <tr>
                                <td colspan="3" align="left">
                                    <span runat="server" class="err" id="msg">&nbsp;</span>
                                </td>
                            </tr>

                            <tr>
                                <td colspan="2" align="center">
                                    <input runat="server" class="btn" type="submit" id="sub" value="Send" style="font-size: larger; padding-left: 20px; padding-right: 20px;"/>
                                </td>
                                <td>&nbsp;</td>
                            </tr>
                        </table>
                        <input type="hidden" id="bg_id" runat="server"/>
                        <input type="hidden" id="short_desc" runat="server"/>
                    </form>
        </table>
    </div>

    <div id="addrs" class="frm" style="display: none; position: absolute;">

        <span style="padding-right: 50px;">Click to select address:</span>

        <a style="float: right; margin-right: 5px;" href="javascript:hide_addrs()">close</a>

        <div style="width: 1px;">&nbsp;</div>
        <select id="addrs_select" size="20" onchange="select_addrs(this)" style="margin-bottom: 5px;">
            <%

                var dictUsersForThisProject = new Dictionary<int, int>();

                // list of email addresses to use.
                if (Session["email_addresses"] == null)
                {
                    if (this.Project > -1)
                    {
                        if (this.Project == 0)
                        {
                            this.Sql = @"select us_id
                    from users
                    where us_active = 1
                    and len(us_email) > 0
                    order by us_email";
                        }
                        else
                        {
                            // Only users explicitly allowed will be listed
                            if (ApplicationSettings.DefaultPermissionLevel == 0)
                                this.Sql = @"select us_id
                        from users
                        where us_active = 1
                        and len(us_email) > 0
                        and us_id in
                            (select pu_user from project_user_xref
                            where pu_project = $pr
                            and pu_permission_level <> 0)
                        order by us_email";
                            // Only users explictly DISallowed will be omitted
                            else
                                this.Sql = @"select us_id
                        from users
                        where us_active = 1
                        and len(us_email) > 0
                        and us_id not in
                            (select pu_user from project_user_xref
                            where pu_project = $pr
                            and pu_permission_level = 0)
                        order by us_email";
                        }

                        this.Sql = this.Sql.Replace("$pr", Convert.ToString(this.Project));
                        var dsUsersForThisProject = DbUtil.GetDataSet(this.Sql);

                        // remember the users for this this project
                        foreach (DataRow dr in dsUsersForThisProject.Tables[0].Rows)
                            dictUsersForThisProject[(int)dr[0]] = 1;
                    }

                    var dtRelatedUsers = Util.GetRelatedUsers(Security, true); // force full names
                                                                                        // let's sort by email
                    var dvRelatedUsers = new DataView(dtRelatedUsers);
                    dvRelatedUsers.Sort = "us_email";

                    var sb = new StringBuilder();

                    foreach (DataRowView drvEmail in dvRelatedUsers)
                        if (dictUsersForThisProject.ContainsKey((int)drvEmail["us_id"]))
                        {
                            var email = (string)drvEmail["us_email"];
                            var username = (string)drvEmail["us_username"];

                            sb.Append("<option style='padding: 3px;'>");

                            if (username != "" && username != email)
                            {
                                sb.Append("\"");
                                sb.Append(username);
                                sb.Append("\"&lt;");
                                sb.Append(email);
                                sb.Append("&gt;");
                            }
                            else
                            {
                                sb.Append(email);
                            }
                            sb.Append("</option>");
                        }

                    Session["email_addresses"] = sb.ToString();
                }

                Response.Write(Session["email_addresses"]);
            %>
        </select>
    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
    <% Response.Write(Application["custom_footer"]); %>
</asp:Content>

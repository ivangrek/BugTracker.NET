<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" ValidateRequest="false" AutoEventWireup="true" CodeBehind="Edit.aspx.cs" Inherits="BugTracker.Web.Bugs.Edit" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>

<%@ Register TagPrefix="BugTracker" TagName="MainMenu" Src="~/Core/Controls/MainMenu.ascx" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <%--TODO <body onload="on_body_load()" onunload="on_body_unload()">--%>

    <link rel="StyleSheet" href="<%= ResolveUrl("~/Scripts/jquery/jquery-ui-1.7.2.custom.css") %>" type="text/css">
    <!-- use btnet_edit_bug.css to control positioning on edit_bug.asp.  use btnet_search.css to control position on Search.aspx  -->
    <link rel="StyleSheet" href="<%= ResolveUrl("~/Content/custom/btnet_edit_bug.css") %>" type="text/css">
    <script type="text/javascript" src="<%= ResolveUrl("~/Scripts/jquery/jquery-ui-1.7.2.custom.min.js") %>"></script>
    <script type="text/javascript" src="<%= ResolveUrl("~/Scripts/jquery/jquery.textarearesizer.compressed.js") %>"></script>
    <script type="text/javascript" src="<%= ResolveUrl("~/Scripts/edit_bug.js") %>"></script>
    <% if (Security.User.UseFckeditor)
        { %>
    <script type="text/javascript" src="<%= ResolveUrl("~/Scripts/ckeditor/ckeditor.js") %>"></script>
    <% } %>
    <script>
        var this_bugid = <% Response.Write(Convert.ToString(this.Id)); %>

            $(document).ready(do_doc_ready);

        function do_doc_ready() {
            date_format = '<% Response.Write(ApplicationSettings.DatepickerDateFormat); %>';
            $(".date").datepicker({ dateFormat: date_format, duration: 'fast' });
            $(".date").change(mark_dirty);
            $(".warn").click(warn_if_dirty);
            $("textarea.resizable:not(.processed)").TextAreaResizer();

            <%

        if (Security.User.UseFckeditor)
        {
            Response.Write("CKEDITOR.replace( 'comment' )");
        }
        else
        {
            Response.Write("$('textarea.resizable2:not(.processed)').TextAreaResizer()");
        }
            %>	
        }
    </script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
    <BugTracker:MainMenu runat="server" ID="MainMenu" />
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" ID="BodyContent" runat="server">
    <div runat="server" id="mainBlock" class="align main">

        <% if (!Security.User.AddsNotAllowed && this.Id > 0)
            { %>
        <a class="warn" href="<%= ResolveUrl("~/Bugs/Edit.aspx?id=0") %>">
            <img src="<%= ResolveUrl("~/Content/images/add.png") %>" border="0" align="top">&nbsp;add new <% Response.Write(ApplicationSettings.SingularBugLabel); %></a>
        &nbsp;&nbsp;&nbsp;&nbsp;
        <% } %>

        <span id="prev_next" runat="server">&nbsp;</span>

        <br>
        <br>

        <table border="0" cellspacing="0" cellpadding="3">
            <tr>
                <td nowrap valign="top">
                    <!-- links -->
                    <div id="edit_bug_menu">
                        <ul>
                            <li id="clone" runat="server" />
                            <li id="print" runat="server" />
                            <li id="merge_bug" runat="server" />
                            <li id="delete_bug" runat="server" />
                            <li id="svn_revisions" runat="server" />
                            <li id="git_commits" runat="server" />
                            <li id="hg_revisions" runat="server" />
                            <li id="subscribers" runat="server" />
                            <li id="subscriptions" runat="server" />
                            <li id="relationships" runat="server" />
                            <li id="tasks" runat="server" />
                            <li id="send_email" runat="server" />
                            <li id="attachment" runat="server" />
                            <li id="custom" runat="server" />
                        </ul>
                    </div>

                    <td nowrap valign="top">
                        <!-- form -->

                        <div id="bugform_div">
                            <form class="frm" runat="server">

                                <% if (ApplicationSettings.DisplayAnotherButtonInEditBugPage)
                                    { %>
                                <div>
                                    <span runat="server" class="err" id="custom_field_msg2">&nbsp;</span>
                                    <span runat="server" class="err" id="msg2">&nbsp;</span>
                                </div>
                                <div style="text-align: center;">
                                    <input
                                        runat="server"
                                        class="btn"
                                        type="submit"
                                        id="submit_button2"
                                        onclick="on_user_hit_submit()"
                                        value="Update" />
                                </div>
                                <% } %>

                                <table border="0" cellpadding="3" cellspacing="0">
                                    <tr>
                                        <td nowrap valign="top">
                                            <span class="lbl" id="bugid_label" runat="server"></span>
                                            <span runat="server" class="bugid" id="bugid"></span>
                                            &nbsp;

    <td valign="top">

        <span class="short_desc_static" id="static_short_desc" runat="server" style="display: none; width: 500px;"></span>


        <input title="" runat="server" type="text" class="short_desc_input" id="short_desc" maxlength="200"
            onkeydown="count_chars('short_desc',200)" onkeyup="count_chars('short_desc',200)" />
        &nbsp;&nbsp;&nbsp;
    <span runat="server" class="err" id="short_desc_err"></span>

        <div class="smallnote" id="short_desc_cnt">&nbsp;</div>

                                </table>
                                <table width="90%" border="0" cellpadding="3" cellspacing="0">
                                    <tr>
                                        <td nowrap>
                                            <span runat="server" id="reported_by"></span>

                                            <% if (this.Id == 0 || this.PermissionLevel == SecurityPermissionLevel.PermissionAll)
                                                { %>
                                        <td nowrap align="right" id="presets">Presets:
    <a title="Use previously saved settings for project, category, priority, etc..."
        href="javascript:get_presets()">use
    </a>
                                            &nbsp;/&nbsp;
    <a title="Save current settings for project, category, priority, etc., so that you can reuse later."
        href="javascript:set_presets()">save
    </a>
                                            <% } %>
                                </table>

                                <table border="0" cellpadding="0" cellspacing="4">

                                    <tr id="tags_row">
                                        <td nowrap>
                                            <span class="lbl" id="tags_label" runat="server">Tags:&nbsp;</span>

                                        <td nowrap>
                                            <span class="stat" id="static_tags" runat="server"></span>
                                            <input runat="server" type="text" class="txt" id="tags" size="70" maxlength="80" onkeydown="mark_dirty()" onkeyup="mark_dirty()" />
                                            <span id="tags_link" runat="server">&nbsp;&nbsp;<a href="javascript:show_tags()">tags</a></span>
                                        <tr id="row1">
                                            <td nowrap>
                                                <span class="lbl" id="project_label" runat="server">Project:&nbsp;</span>
                                            <td nowrap>
                                                <span class="stat" id="static_project" runat="server"></span>

                                                <asp:DropDownList ID="project" runat="server"
                                                    AutoPostBack="True">
                                                </asp:DropDownList>
                                            <tr id="row2">
                                                <td nowrap>
                                                    <span class="lbl" id="org_label" runat="server">Organization:&nbsp;</span>
                                                <td nowrap>
                                                    <span class="stat" id="static_org" runat="server"></span>
                                                    <asp:DropDownList ID="org" runat="server"></asp:DropDownList>
                                                <tr id="row3">
                                                    <td nowrap>
                                                        <span class="lbl" id="category_label" runat="server">Category:&nbsp;</span>
                                                    <td nowrap>
                                                        <span class="stat" id="static_category" runat="server"></span>
                                                        <asp:DropDownList ID="category" runat="server"></asp:DropDownList>
                                                    <tr id="row4">
                                                        <td nowrap>
                                                            <span class="lbl" id="priority_label" runat="server">Priority:&nbsp;</span>
                                                        <td nowrap>
                                                            <span class="stat" id="static_priority" runat="server"></span>
                                                            <asp:DropDownList ID="priority" runat="server"></asp:DropDownList>
                                                        <tr id="row5">
                                                            <td nowrap>
                                                                <span class="lbl" id="assigned_to_label" runat="server">Assigned to:&nbsp;</span>
                                                            <td nowrap>
                                                                <span class="stat" id="static_assigned_to" runat="server"></span>
                                                                <asp:DropDownList ID="assigned_to" runat="server"></asp:DropDownList>
                                                                &nbsp;
    <span runat="server" class="err" id="assigned_to_err"></span>

                                                                <tr id="row6">
                                                                    <td nowrap>
                                                                        <span class="lbl" id="status_label" runat="server">Status:&nbsp;</span>
                                                                    <td nowrap>
                                                                        <span class="stat" id="static_status" runat="server"></span>
                                                                        <asp:DropDownList ID="status" runat="server"></asp:DropDownList>

                                                                        <% if (ApplicationSettings.ShowUserDefinedBugAttribute)
                                                                            { %>
                                                                    <tr id="row7">
                                                                        <td nowrap>
                                                                            <span class="lbl" id="udf_label" runat="server">
                                                                                <% Response.Write(ApplicationSettings.UserDefinedBugAttributeName); %>:&nbsp;
                                                                            </span>
                                                                            <td nowrap>
                                                                                <span class="stat" id="static_udf" runat="server"></span>
                                                                                <asp:DropDownList ID="udf" runat="server">
                                                                                </asp:DropDownList>
                                                                                <% } %>


                                                                                <%
                                                                                    display_custom_fields(Security);
                                                                                    display_project_specific_custom_fields(Security);
                                                                                %>
                                </table>

                                <table border="0" cellpadding="0" cellspacing="3" width="98%">

                                    <tr>
                                        <td nowrap>&nbsp;
    <span id="comment_label" runat="server">Comment:</span>

                                            <span class="smallnote" style="margin-left: 170px">
                                                <%
                                                    if (this.PermissionLevel != SecurityPermissionLevel.PermissionReadonly)
                                                    {
                                                        Response.Write("Entering \""
                                                                       + ApplicationSettings.BugLinkMarker
                                                                       + "999\" in comment creates link to id 999");
                                                    }
                                                %>
                                            </span>
                                            <br>
                                            <textarea id="comment" rows="5" cols="100" runat="server" class="txt resizable2" onkeydown="mark_dirty()" onkeyup="mark_dirty()"></textarea>
                                        <tr>
                                            <td nowrap>
                                                <asp:CheckBox runat="server" class="cb" ID="internal_only" />
                                                <span runat="server" id="internal_only_label">Comment visible to internal users only</span>
                                            <tr>
                                                <td nowrap align="left">
                                                    <span runat="server" class="err" id="custom_field_msg">&nbsp;</span>
                                                    <span runat="server" class="err" id="custom_validation_err_msg">&nbsp;</span>
                                                    <span runat="server" class="err" id="msg">&nbsp;</span>
                                                <tr>
                                                    <td nowrap align="center">
                                                        <input
                                                            runat="server"
                                                            class="btn"
                                                            type="submit"
                                                            id="submit_button"
                                                            onclick="on_user_hit_submit()"
                                                            value="Update" />
                                </table>

                                <input type="hidden" id="new_id" runat="server" value="0" />
                                <input type="hidden" id="prev_short_desc" runat="server" />
                                <input type="hidden" id="prev_tags" runat="server" />
                                <input type="hidden" id="prev_project" runat="server" />
                                <input type="hidden" id="prev_project_name" runat="server" />
                                <input type="hidden" id="prev_org" runat="server" />
                                <input type="hidden" id="prev_org_name" runat="server" />
                                <input type="hidden" id="prev_category" runat="server" />
                                <input type="hidden" id="prev_priority" runat="server" />
                                <input type="hidden" id="prev_assigned_to" runat="server" />
                                <input type="hidden" id="prev_assigned_to_username" runat="server" />
                                <input type="hidden" id="prev_status" runat="server" />
                                <input type="hidden" id="prev_udf" runat="server" />
                                <input type="hidden" id="prev_pcd1" runat="server" />
                                <input type="hidden" id="prev_pcd2" runat="server" />
                                <input type="hidden" id="prev_pcd3" runat="server" />
                                <input type="hidden" id="snapshot_timestamp" runat="server" />
                                <input type="hidden" id="clone_ignore_bugid" runat="server" value="0" />
                                <input type="hidden" id="user_hit_submit" name="user_hit_submit" value="0" />

                                <%
                                    if (this.Id != 0)
                                    {
                                        display_bug_relationships(Security);
                                    }
                                %>
                            </form>
                        </div>
                        <!-- bug form div -->
        </table>

        <br>
        <span id="toggle_images" runat="server"></span>
        &nbsp;&nbsp;&nbsp;&nbsp;
        <span id="toggle_history" runat="server"></span>
        <br>
        <br>

        <div id="posts">

            <%
                // COMMENTS
                if (this.Id != 0)
                {
                    PrintBug.WritePosts(
                        this.DsPosts,
                        Response,
                        this.Id,
                        this.PermissionLevel,
                        true, // write links
                        this.ImagesInline,
                        this.HistoryInline,
                        true, // internal_posts
                        Security.User);
                }
            %>
        </div>
        <!-- posts -->
    </div>

    <div runat="server" id="errorBlock" class="align" Visible="False">
        <div class="err">
            <%= Util.CapitalizeFirstLetter(ApplicationSettings.SingularBugLabel) %>
                not found:&nbsp;<%= Convert.ToString(this.Id)%>
        </div>

        <p></p>

        <a href='<%= ResolveUrl("~/Bugs/List.aspx")%>'>View
             <%= ApplicationSettings.PluralBugLabel%>
        </a>
    </div>

    <div runat="server" id="errorBlockPermissions" class="align" Visible="False">
        <div class="err">
            You are not allowed to view this <%= ApplicationSettings.SingularBugLabel %>
            not found:&nbsp;<%= Convert.ToString(this.Id)%>
        </div>

        <p></p>

        <a href='<%= ResolveUrl("~/Bugs/List.aspx")%>'>View
            <%= Util.CapitalizeFirstLetter(ApplicationSettings.PluralBugLabel) %>
        </a>
    </div>

    <div runat="server" id="errorBlockIntegerId" class="align" Visible="False">
        <div class="err">
            Error: <%= Util.CapitalizeFirstLetter(ApplicationSettings.SingularBugLabel) %> ID must be an integer.
        </div>

        <p></p>

        <a href='<%= ResolveUrl("~/Bugs/List.aspx")%>'>View
            <%= Util.CapitalizeFirstLetter(ApplicationSettings.PluralBugLabel) %>
        </a>
    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
    <% Response.Write(Application["custom_footer"]); %>
</asp:Content>

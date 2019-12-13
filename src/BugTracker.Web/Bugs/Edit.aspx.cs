/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Bugs
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text.RegularExpressions;
    using System.Web;
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using Core;

    public partial class Edit : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public ISecurity Security { get; set; }

        protected bool AssignedToChanged;

        protected string CommentFormated;
        protected string CommentSearch;
        protected string CommentType;
        protected DataRow DrBug;

        protected DataSet DsCustomCols;
        protected DataSet DsPosts { get; set; }
        protected DataTable DtUsers { get; set; }

        protected bool Good = true;
        protected SortedDictionary<string, string> HashCustomCols = new SortedDictionary<string, string>();
        protected bool HistoryInline;
        protected int Id;

        protected bool ImagesInline = true;
        protected SecurityPermissionLevel PermissionLevel { get; set; }
        protected string Sql {get; set; }
        protected bool StatusChanged;

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.AnyUserOk);

            this.MainMenu.SelectedItem = ApplicationSettings.PluralBugLabel;

            set_msg("");
            set_custom_field_msg("");

            var stringBugid = Request["id"];
            if (stringBugid == null || stringBugid == "0" ||
                stringBugid != "0" && this.clone_ignore_bugid.Value == "1")
            {
                // New
                this.Id = 0;
                this.bugid_label.InnerHtml = "Description:&nbsp;";
            }
            else
            {
                if (!Util.IsInt(stringBugid))
                {
                    this.mainBlock.Visible = false;
                    this.errorBlock.Visible = false;
                    this.errorBlockPermissions.Visible = false;
                    this.errorBlockIntegerId.Visible = true;

                    return;
                }

                // Existing
                this.Id = Convert.ToInt32(stringBugid);

                this.bugid_label.Visible = true;
                this.bugid_label.InnerHtml = Util.CapitalizeFirstLetter(ApplicationSettings.SingularBugLabel) + " ID:&nbsp;";
            }

            // Get list of custom fields

            this.DsCustomCols = Util.GetCustomColumns();

            if (!IsPostBack)
            {
                // Fetch stuff from db and put on page

                if (this.Id == 0)
                {
                    prepare_for_insert(Security);
                }
                else
                {
                    get_cookie_values_for_show_hide_toggles();

                    // Get this entry's data from the db and fill in the form
                    this.DrBug = Bug.GetBugDataRow(this.Id, Security, this.DsCustomCols);

                    prepare_for_update(Security);
                }

                if (Security.User.ExternalUser || !ApplicationSettings.EnableInternalOnlyPosts)
                {
                    this.internal_only.Visible = false;
                    this.internal_only_label.Visible = false;
                }
            }
            else
            {
                get_cookie_values_for_show_hide_toggles();

                this.DrBug = Bug.GetBugDataRow(this.Id, Security, this.DsCustomCols);

                load_incoming_custom_col_vals_into_hash(Security);

                if (did_user_hit_submit_button()) // or is this a project dropdown autopostback?
                {
                    this.Good = validate(Security);

                    if (this.Good)
                    {
                        // Actually do the update
                        if (this.Id == 0)
                            do_insert(Security);
                        else
                            do_update(Security);
                    }
                    else // bad, invalid
                    {
                        // Say we didn't do anything.
                        if (this.Id == 0)
                            set_msg(Util.CapitalizeFirstLetter(ApplicationSettings.SingularBugLabel) + " was not created.");
                        else
                            set_msg(Util.CapitalizeFirstLetter(ApplicationSettings.SingularBugLabel) + " was not updated.");
                        load_user_dropdown(Security);
                    }
                }
                else
                {
                    // This is the project dropdown autopost back.
                    load_user_dropdown(Security);
                }
            }
        }

        public bool did_user_hit_submit_button()
        {
            var val = Request["user_hit_submit"];
            return val == "1";
        }

        public void get_cookie_values_for_show_hide_toggles()
        {
            var cookie = Request.Cookies["images_inline"];
            if (cookie == null || cookie.Value == "0")
                this.ImagesInline = false;
            else
                this.ImagesInline = true;

            cookie = Request.Cookies["history_inline"];
            if (cookie == null || cookie.Value == "0")
                this.HistoryInline = false;
            else
                this.HistoryInline = true;
        }

        public void prepare_for_insert(ISecurity security)
        {
            if (Security.User.AddsNotAllowed)
            {
                // TODO wrong message
                this.mainBlock.Visible = false;
                this.errorBlock.Visible = true;

                return;
            }

            Page.Title = $"{ApplicationSettings.AppTitle} - Create ";
            Page.Title += Util.CapitalizeFirstLetter(ApplicationSettings.SingularBugLabel);

            this.submit_button.Value = "Create";

            if (ApplicationSettings.DisplayAnotherButtonInEditBugPage)
            {
                this.submit_button2.Value = "Create";
            }

            load_dropdowns_for_insert(security);

            // Prepare for custom columns
            foreach (DataRow drcc in this.DsCustomCols.Tables[0].Rows)
            {
                var columnName = (string) drcc["name"];
                if (Security.User.DictCustomFieldPermissionLevel[columnName] != SecurityPermissionLevel.PermissionNone)
                {
                    var defaultval = get_custom_col_default_value(drcc["default value"]);
                    this.HashCustomCols.Add(columnName, defaultval);
                }
            }

            // We don't know the project yet, so all permissions
            set_controls_field_permission(SecurityPermissionLevel.PermissionAll, security);

            // Execute code not written by me
            Workflow.CustomAdjustControls(null, security.User, this);
        }

        public void load_dropdowns_for_insert(ISecurity security)
        {
            load_dropdowns(security.User, security);

            // Get the defaults
            Sql = "\nselect top 1 pj_id from projects where pj_default = 1 order by pj_name;"; // 0
            Sql += "\nselect top 1 ct_id from categories where ct_default = 1 order by ct_name;"; // 1
            Sql += "\nselect top 1 pr_id from priorities where pr_default = 1 order by pr_name;"; // 2
            Sql += "\nselect top 1 st_id from statuses where st_default = 1 order by st_name;"; // 3
            Sql +=
                "\nselect top 1 udf_id from user_defined_attribute where udf_default = 1 order by udf_name;"; // 4

            var dsDefaults = DbUtil.GetDataSet(Sql);

            load_project_and_user_dropdown_for_insert(dsDefaults.Tables[0], security);

            load_other_dropdowns_and_select_defaults(dsDefaults, security);
        }

        public void prepare_for_update(ISecurity security)
        {
            if (this.DrBug == null)
            {
                this.mainBlock.Visible = false;
                this.errorBlock.Visible = true;
                this.errorBlockPermissions.Visible = false;
                this.errorBlockIntegerId.Visible = false;

                return;
            }

            // look at permission level and react accordingly
            PermissionLevel = (SecurityPermissionLevel)(int) this.DrBug["pu_permission_level"];

            if (PermissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                this.mainBlock.Visible = false;
                this.errorBlock.Visible = false;
                this.errorBlockPermissions.Visible = true;
                this.errorBlockIntegerId.Visible = false;

                return;
            }

            foreach (DataRow drcc in this.DsCustomCols.Tables[0].Rows)
            {
                var columnName = (string) drcc["name"];
                if (Security.User.DictCustomFieldPermissionLevel[columnName] != SecurityPermissionLevel.PermissionNone)
                {
                    var val = Util.FormatDbValue(this.DrBug[columnName]);
                    this.HashCustomCols.Add(columnName, val);
                }
            }

            // move stuff to the page

            this.bugid.InnerText = Convert.ToString((int) this.DrBug["id"]);

            // Fill in this form
            this.short_desc.Value = (string) this.DrBug["short_desc"];
            this.tags.Value = (string) this.DrBug["bg_tags"];
            Page.Title = Util.CapitalizeFirstLetter(ApplicationSettings.SingularBugLabel)
                         + " ID " + Convert.ToString(this.DrBug["id"]) + " " + (string) this.DrBug["short_desc"];

            // reported by
            string s;
            s = "Created by ";
            s += PrintBug.FormatEmailUserName(
                true,
                Convert.ToInt32(this.DrBug["id"]), PermissionLevel,
                Convert.ToString(this.DrBug["reporter_email"]),
                Convert.ToString(this.DrBug["reporter"]),
                Convert.ToString(this.DrBug["reporter_fullname"]));
            s += " on ";
            s += Util.FormatDbDateTime(this.DrBug["reported_date"]);
            s += ", ";
            s += Util.HowLongAgo((int) this.DrBug["seconds_ago"]);

            this.reported_by.InnerHtml = s;

            // save current values in previous, so that later we can write the audit trail when things change
            this.prev_short_desc.Value = (string) this.DrBug["short_desc"];
            this.prev_tags.Value = (string) this.DrBug["bg_tags"];
            this.prev_project.Value = Convert.ToString((int) this.DrBug["project"]);
            this.prev_project_name.Value = Convert.ToString(this.DrBug["current_project"]);
            this.prev_org.Value = Convert.ToString((int) this.DrBug["organization"]);
            this.prev_org_name.Value = Convert.ToString(this.DrBug["og_name"]);
            this.prev_category.Value = Convert.ToString((int) this.DrBug["category"]);
            this.prev_priority.Value = Convert.ToString((int) this.DrBug["priority"]);
            this.prev_assigned_to.Value = Convert.ToString((int) this.DrBug["assigned_to_user"]);
            this.prev_assigned_to_username.Value = Convert.ToString(this.DrBug["assigned_to_username"]);
            this.prev_status.Value = Convert.ToString((int) this.DrBug["status"]);
            this.prev_udf.Value = Convert.ToString((int) this.DrBug["udf"]);
            this.prev_pcd1.Value = (string) this.DrBug["bg_project_custom_dropdown_value1"];
            this.prev_pcd2.Value = (string) this.DrBug["bg_project_custom_dropdown_value2"];
            this.prev_pcd3.Value = (string) this.DrBug["bg_project_custom_dropdown_value3"];

            load_dropdowns_for_update(security);

            load_project_and_user_dropdown_for_update(security); // must come before set_controls_field_permission, after assigning to prev_ values

            set_controls_field_permission(PermissionLevel, security);

            this.snapshot_timestamp.Value = Convert.ToDateTime(this.DrBug["snapshot_timestamp"])
                .ToString("yyyyMMdd HH\\:mm\\:ss\\:fff");

            prepare_a_bunch_of_links_for_update(security);

            format_prev_next_bug();

            // save for next bug
            if (this.project.SelectedItem != null) Session["project"] = this.project.SelectedItem.Value;

            // Execute code not written by me
            Workflow.CustomAdjustControls(this.DrBug, security.User, this);
        }

        public void prepare_a_bunch_of_links_for_update(ISecurity security)
        {
            var toggleImagesLink = "<a href='javascript:toggle_images2("
                                     + Convert.ToString(this.Id) + ")'><span id=hideshow_images>"
                                     + (this.ImagesInline ? "hide" : "show")
                                     + " inline images"
                                     + "</span></a>";
            this.toggle_images.InnerHtml = toggleImagesLink;

            var toggleHistoryLink = "<a href='javascript:toggle_history2("
                                      + Convert.ToString(this.Id) + ")'><span id=hideshow_history>"
                                      + (this.HistoryInline ? "hide" : "show")
                                      + " change history"
                                      + "</span></a>";
            this.toggle_history.InnerHtml = toggleHistoryLink;

            if (PermissionLevel == SecurityPermissionLevel.PermissionAll)
            {
                var cloneLink = "<a class=warn href=\"javascript:clone()\" "
                                 + " title='Create a copy of this item'><img src=" + ResolveUrl("~/Content/images/paste_plain.png") + " border=0 align=top>&nbsp;create copy</a>";
                this.clone.InnerHtml = cloneLink;
            }

            if (PermissionLevel != SecurityPermissionLevel.PermissionReadonly)
            {
                var attachmentLink =
                    "<img src=" + ResolveUrl("~/Content/images/attach.gif") + " align=top>&nbsp;<a href=\"javascript:open_popup_window('" + ResolveUrl("~/Attachment/Create") +@"','add attachment ',"
                    + Convert.ToString(this.Id)
                    + ",700,320)\" title='Attach an image, document, or other file to this item'>add attachment</a>";
                this.attachment.InnerHtml = attachmentLink;
            }
            else
            {
                this.attachment.Visible = false;
            }

            if (!Security.User.IsGuest)
            {
                if (PermissionLevel != SecurityPermissionLevel.PermissionReadonly)
                {
                    var sendEmailLink = "<a href='javascript:send_email("
                                          + Convert.ToString(this.Id)
                                          + ")' title='Send an email about this item'><img src=" + ResolveUrl("~/Content/images/email_edit.png") + " border=0 align=top>&nbsp;send email</a>";
                    this.send_email.InnerHtml = sendEmailLink;
                }
                else
                {
                    this.send_email.Visible = false;
                }
            }
            else
            {
                this.send_email.Visible = false;
            }

            if (PermissionLevel != SecurityPermissionLevel.PermissionReadonly)
            {
                var subscribersLink = "<a target=_blank href='" + ResolveUrl($"~/Bug/ViewSubscriber/{this.Id}")
                                       + "' title='View users who have subscribed to email notifications for this item'><img src=" + ResolveUrl("~/Content/images/telephone_edit.png") + " border=0 align=top>&nbsp;subscribers</a>";
                this.subscribers.InnerHtml = subscribersLink;
            }
            else
            {
                this.subscribers.Visible = false;
            }

            if (ApplicationSettings.EnableRelationships)
            {
                var relationshipCnt = 0;
                if (this.Id != 0) relationshipCnt = (int) this.DrBug["relationship_cnt"];
                var relationshipsLink = "<a target=_blank href=/Bug/Relationship?bugId="
                                         + Convert.ToString(this.Id)
                                         + " title='Create a relationship between this item and another item'><img src=" + ResolveUrl("~/Content/images/database_link.png") + " border=0 align=top>&nbsp;relationships(<span id=relationship_cnt>" +
                                         relationshipCnt + "</span>)</a>";
                this.relationships.InnerHtml = relationshipsLink;
            }
            else
            {
                this.relationships.Visible = false;
            }

            if (ApplicationSettings.EnableSubversionIntegration)
            {
                var revisionCnt = 0;
                if (this.Id != 0) revisionCnt = (int) this.DrBug["svn_revision_cnt"];
                var svnRevisionsLink = "<a target=_blank href=" + ResolveUrl("~/Versioning/Svn/Index/") + @""
                                         + Convert.ToString(this.Id)
                                         + " title='View Subversion svn_revisions related to this item'><img src=" + ResolveUrl("~/Content/images/svn.png") + " border=0 align=top>&nbsp;svn revisions(" +
                                         revisionCnt + ")</a>";
                this.svn_revisions.InnerHtml = svnRevisionsLink;
            }
            else
            {
                this.svn_revisions.Visible = false;
            }

            if (ApplicationSettings.EnableGitIntegration)
            {
                var revisionCnt = 0;
                if (this.Id != 0) revisionCnt = (int) this.DrBug["git_commit_cnt"];
                var gitCommitsLink = "<a target=_blank href=" + ResolveUrl("~/Versioning/Git/Index/") + @""
                                       + Convert.ToString(this.Id)
                                       + " title='View git git_commits related to this item'><img src=" + ResolveUrl("~/Content/images/git.png") + " border=0 align=top>&nbsp;git commits(" +
                                       revisionCnt + ")</a>";
                this.git_commits.InnerHtml = gitCommitsLink;
            }
            else
            {
                this.git_commits.Visible = false;
            }

            if (ApplicationSettings.EnableMercurialIntegration)
            {
                var revisionCnt = 0;
                if (this.Id != 0) revisionCnt = (int) this.DrBug["hg_commit_cnt"];
                var hgRevisionsLink = "<a target=_blank href=" + ResolveUrl("~/Versioning/Hg/Index/") + @""
                                        + Convert.ToString(this.Id)
                                        + " title='View mercurial git_hg_revisions related to this item'><img src=" + ResolveUrl("~/Content/images/hg.png") + " border=0 align=top>&nbsp;hg revisions(" +
                                        revisionCnt + ")</a>";
                this.hg_revisions.InnerHtml = hgRevisionsLink;
            }
            else
            {
                this.hg_revisions.Visible = false;
            }

            if (Security.User.IsAdmin || Security.User.CanViewTasks)
            {
                if (ApplicationSettings.EnableTasks)
                {
                    var taskCnt = 0;
                    if (this.Id != 0) taskCnt = (int) this.DrBug["task_cnt"];
                    var tasksLink = "<a target=_blank href=/Task?bugId="
                                     + Convert.ToString(this.Id)
                                     + " title='View sub-tasks/time-tracking entries related to this item'><img src=" + ResolveUrl("~/Content/images/clock.png") + " border=0 align=top>&nbsp;tasks/time(<span id=task_cnt>" +
                                     taskCnt + "</span>)</a>";
                    this.tasks.InnerHtml = tasksLink;
                }
                else
                {
                    this.tasks.Visible = false;
                }
            }
            else
            {
                this.tasks.Visible = false;
            }

            format_subcribe_cancel_link(security);

            this.print.InnerHtml = "<a target=_blank href=" + ResolveUrl("~/Bug/PrintDetail?id=")
                                   + Convert.ToString(this.Id)
                                   + " title='Display this item in a printer-friendly format'><img src=" + ResolveUrl("~/Content/images/printer.png") + " border=0 align=top>&nbsp;print</a>";

            // merge
            if (!Security.User.IsGuest)
            {
                if (Security.User.IsAdmin
                    || Security.User.CanMergeBugs)
                {
                    var mergeBugLink = "<a href=" + ResolveUrl("~/Bug/Merge/")
                                         + Convert.ToString(this.Id)
                                         + " title='Merge this item and another item together'><img src=" + ResolveUrl("~/Content/images/database_refresh.png") + " border=0 align=top>&nbsp;merge</a>";

                    this.merge_bug.InnerHtml = mergeBugLink;
                }
                else
                {
                    this.merge_bug.Visible = false;
                }
            }
            else
            {
                this.merge_bug.Visible = false;
            }

            // delete 
            if (!Security.User.IsGuest)
            {
                if (Security.User.IsAdmin
                    || Security.User.CanDeleteBug)
                {
                    var deleteBugLink = "<a href='" + ResolveUrl($"~/Bug/Delete/{this.Id}")
                                          + "' title='Delete this item'><img src=" + ResolveUrl("~/Content/images/delete.png") + " border=0 align=top>&nbsp;delete</a>";

                    this.delete_bug.InnerHtml = deleteBugLink;
                }
                else
                {
                    this.delete_bug.Visible = false;
                }
            }
            else
            {
                this.delete_bug.Visible = false;
            }

            // custom bug link
            if (!string.IsNullOrEmpty(ApplicationSettings.CustomBugLinkLabel))
            {
                var customBugLink = "<a href="
                                      + ApplicationSettings.CustomBugLinkUrl
                                      + "?bugid="
                                      + Convert.ToString(this.Id)
                                      + "><img src=" + ResolveUrl("~/Content/images/brick.png") + " border=0 align=top>&nbsp;"
                                      + ApplicationSettings.CustomBugLinkLabel
                                      + "</a>";

                this.custom.InnerHtml = customBugLink;
            }
            else
            {
                this.custom.Visible = false;
            }
        }

        public void load_dropdowns_for_update(ISecurity security)
        {
            load_dropdowns(security.User, security);

            // select the dropdowns

            foreach (ListItem li in this.category.Items)
                if (Convert.ToInt32(li.Value) == (int) this.DrBug["category"])
                    li.Selected = true;
                else
                    li.Selected = false;

            foreach (ListItem li in this.priority.Items)
                if (Convert.ToInt32(li.Value) == (int) this.DrBug["priority"])
                    li.Selected = true;
                else
                    li.Selected = false;

            foreach (ListItem li in this.status.Items)
                if (Convert.ToInt32(li.Value) == (int) this.DrBug["status"])
                    li.Selected = true;
                else
                    li.Selected = false;

            foreach (ListItem li in this.udf.Items)
                if (Convert.ToInt32(li.Value) == (int) this.DrBug["udf"])
                    li.Selected = true;
                else
                    li.Selected = false;

            // special logic for org
            if (this.Id != 0)
            {
                // Org
                if (this.prev_org.Value != "0")
                {
                    var alreadyInDropdown = false;
                    foreach (ListItem li in this.org.Items)
                        if (li.Value == this.prev_org.Value)
                        {
                            alreadyInDropdown = true;
                            break;
                        }

                    // Add to the list, even if permissions don't allow it now, because, in the past, they did allow it.
                    if (!alreadyInDropdown)
                        this.org.Items.Add(
                            new ListItem(this.prev_org_name.Value, this.prev_org.Value));
                }

                foreach (ListItem li in this.org.Items)
                    if (li.Value == this.prev_org.Value)
                        li.Selected = true;
                    else
                        li.Selected = false;
            }
        }

        public void get_comment_text_from_control(ISecurity security)
        {
            if (Security.User.UseFckeditor)
            {
                this.CommentFormated = Util.StripDangerousTags(this.comment.Value);
                this.CommentSearch = Util.StripHtml(this.comment.Value);
                this.CommentType = "text/html";
            }
            else
            {
                this.CommentFormated = HttpUtility.HtmlDecode(this.comment.Value);
                this.CommentSearch = this.CommentFormated;
                this.CommentType = "text/plain";
            }
        }

        public void load_incoming_custom_col_vals_into_hash(ISecurity security)
        {
            // Fetch the values of the custom columns from the Request and stash them in a hash table.

            foreach (DataRow drcc in this.DsCustomCols.Tables[0].Rows)
            {
                var columnName = (string) drcc["name"];

                if (Security.User.DictCustomFieldPermissionLevel[columnName] != SecurityPermissionLevel.PermissionNone) this.HashCustomCols.Add(columnName, Request[columnName]);
            }
        }

        public void do_insert(ISecurity security)
        {
            get_comment_text_from_control(security);

            // Project specific
            var pcd1 = Request["pcd1"];
            var pcd2 = Request["pcd2"];
            var pcd3 = Request["pcd3"];

            if (pcd1 == null) pcd1 = string.Empty;
            if (pcd2 == null) pcd2 = string.Empty;
            if (pcd3 == null) pcd3 = string.Empty;

            pcd1 = pcd1.Replace("'", "''");
            pcd2 = pcd2.Replace("'", "''");
            pcd3 = pcd3.Replace("'", "''");

            var newIds = Bug.InsertBug(this.short_desc.Value, security, this.tags.Value,
                Convert.ToInt32(this.project.SelectedItem.Value),
                Convert.ToInt32(this.org.SelectedItem.Value),
                Convert.ToInt32(this.category.SelectedItem.Value),
                Convert.ToInt32(this.priority.SelectedItem.Value),
                Convert.ToInt32(this.status.SelectedItem.Value),
                Convert.ToInt32(this.assigned_to.SelectedItem.Value),
                Convert.ToInt32(this.udf.SelectedItem.Value),
                pcd1,
                pcd2,
                pcd3, this.CommentFormated, this.CommentSearch,
                null, // from
                null, // cc
                this.CommentType, this.internal_only.Checked, this.HashCustomCols,
                true); // send notifications

            if (!string.IsNullOrEmpty(this.tags.Value) && ApplicationSettings.EnableTags)
            {
                Tags.BuildTagIndex(Application);
            }

            this.Id = newIds.Bugid;

            WhatsNew.AddNews(this.Id, this.short_desc.Value, "added", security);

            this.new_id.Value = Convert.ToString(this.Id);
            set_msg(Util.CapitalizeFirstLetter(ApplicationSettings.SingularBugLabel) + " was created.");

            // save for next bug
            Session["project"] = this.project.SelectedItem.Value;

            Response.Redirect($"~/Bugs/Edit.aspx?id={this.Id}");
        }

        public void do_update(ISecurity security)
        {
            PermissionLevel = fetch_permission_level(this.project.SelectedItem.Value, security);

            //if (project.SelectedItem.Value == prev_project.Value)
            //{
            //    set_controls_field_permission(permission_level);
            //}

            var bugFieldsHaveChanged = false;
            var bugpostFieldsHaveChanged = false;

            get_comment_text_from_control(security);

            string newProject;
            if (this.project.SelectedItem.Value != this.prev_project.Value)
            {
                newProject = this.project.SelectedItem.Value;
                var permissionLevelOnNewProject = fetch_permission_level(newProject, security);
                if (SecurityPermissionLevel.PermissionNone == permissionLevelOnNewProject
                    || SecurityPermissionLevel.PermissionReadonly == permissionLevelOnNewProject)
                {
                    set_msg(Util.CapitalizeFirstLetter(ApplicationSettings.SingularBugLabel)
                            + " was not updated. You do not have the necessary permissions to change this "
                            + ApplicationSettings.SingularBugLabel + " to the specified Project.");
                    return;
                }

                PermissionLevel = permissionLevelOnNewProject;
            }
            else
            {
                newProject = Util.SanitizeInteger(this.prev_project.Value);
            }

            Sql = @"declare @now datetime
        declare @last_updated datetime
        select @last_updated = bg_last_updated_date from bugs where bg_id = $id
        if @last_updated > '$snapshot_datetime'
        begin
            -- signal that we did NOT do the update
            set @now = '$snapshot_datetime'
        end
        else
        begin
            -- signal that we DID do the update
            set @now = getdate()

            update bugs set
            bg_short_desc = N'$sd',
            bg_tags = N'$tags',
            bg_project = $pj,
            bg_org = $og,
            bg_category = $ct,
            bg_priority = $pr,
            bg_assigned_to_user = $au,
            bg_status = $st,
            bg_last_updated_user = $lu,
            bg_last_updated_date = @now,
            bg_user_defined_attribute = $udf
            $pcd_placeholder	
            $custom_cols_placeholder
            where bg_id = $id
        end
        select @now";

            Sql = Sql.Replace("$sd", this.short_desc.Value.Replace("'", "''"));
            Sql = Sql.Replace("$tags", this.tags.Value.Replace("'", "''"));
            Sql = Sql.Replace("$lu", Convert.ToString(Security.User.Usid));
            Sql = Sql.Replace("$id", Convert.ToString(this.Id));
            Sql = Sql.Replace("$pj", newProject);
            Sql = Sql.Replace("$og", this.org.SelectedItem.Value);
            Sql = Sql.Replace("$ct", this.category.SelectedItem.Value);
            Sql = Sql.Replace("$pr", this.priority.SelectedItem.Value);
            Sql = Sql.Replace("$au", this.assigned_to.SelectedItem.Value);
            Sql = Sql.Replace("$st", this.status.SelectedItem.Value);
            Sql = Sql.Replace("$udf", this.udf.SelectedItem.Value);
            Sql = Sql.Replace("$snapshot_datetime", this.snapshot_timestamp.Value);

            if (PermissionLevel == SecurityPermissionLevel.PermissionReadonly
                || PermissionLevel == SecurityPermissionLevel.PermissionReporter)
            {
                Sql = Sql.Replace("$pcd_placeholder", "");
            }
            else
            {
                Sql = Sql.Replace("$pcd_placeholder", @",
bg_project_custom_dropdown_value1 = N'$pcd1',
bg_project_custom_dropdown_value2 = N'$pcd2',
bg_project_custom_dropdown_value3 = N'$pcd3'
");

                var pcd1 = Request["pcd1"];
                var pcd2 = Request["pcd2"];
                var pcd3 = Request["pcd3"];

                if (pcd1 == null) pcd1 = string.Empty;
                if (pcd2 == null) pcd2 = string.Empty;
                if (pcd3 == null) pcd3 = string.Empty;

                Sql = Sql.Replace("$pcd1", pcd1.Replace("'", "''"));
                Sql = Sql.Replace("$pcd2", pcd2.Replace("'", "''"));
                Sql = Sql.Replace("$pcd3", pcd3.Replace("'", "''"));
            }

            if (this.DsCustomCols.Tables[0].Rows.Count == 0 || PermissionLevel != SecurityPermissionLevel.PermissionAll)
            {
                Sql = Sql.Replace("$custom_cols_placeholder", "");
            }
            else
            {
                var customColsSql = string.Empty;

                foreach (DataRow drcc in this.DsCustomCols.Tables[0].Rows)
                {
                    var columnName = (string) drcc["name"];

                    // if we've made customizations that cause the field to not come back to us,
                    // don't replace something with null
                    var o = Request[columnName];
                    if (o == null) continue;

                    // skip if no permission to update
                    if (Security.User.DictCustomFieldPermissionLevel[columnName] !=
                        SecurityPermissionLevel.PermissionAll) continue;

                    customColsSql += ",[" + columnName + "]";
                    customColsSql += " = ";

                    var datatype = (string) drcc["datatype"];

                    var customColVal = Util.RequestToStringForSql(
                        Request[columnName],
                        datatype);

                    customColsSql += customColVal;
                }

                Sql = Sql.Replace("$custom_cols_placeholder", customColsSql);
            }

            var lastUpdateDate = (DateTime) DbUtil.ExecuteScalar(Sql);

            WhatsNew.AddNews(this.Id, this.short_desc.Value, "updated", security);

            var dateFromDb = lastUpdateDate.ToString("yyyyMMdd HH\\:mm\\:ss\\:fff");
            var dateFromWebpage = this.snapshot_timestamp.Value;

            if (dateFromDb != dateFromWebpage)
            {
                this.snapshot_timestamp.Value = dateFromDb;
                Bug.AutoSubscribe(this.Id);
                format_subcribe_cancel_link(security);
                bugFieldsHaveChanged = record_changes(security);
            }
            else
            {
                set_msg(Util.CapitalizeFirstLetter(ApplicationSettings.SingularBugLabel)
                        + " was NOT updated.<br>"
                        + " Somebody changed it while you were editing it.<br>"
                        + " Click <a href=" + ResolveUrl("~/Bugs/Edit.aspx") + "?id="
                        + Convert.ToString(this.Id)
                        + ">[here]</a> to refresh the page and discard your changes.<br>");
                return;
            }

            bugpostFieldsHaveChanged = Bug.InsertComment(this.Id, Security.User.Usid, this.CommentFormated, this.CommentSearch,
                                              null, // from
                                              null, // cc
                                              this.CommentType, this.internal_only.Checked) != 0;

            if (bugFieldsHaveChanged || bugpostFieldsHaveChanged && !this.internal_only.Checked)
                Bug.SendNotifications(Bug.Update, this.Id, security, 0, this.StatusChanged, this.AssignedToChanged,
                    Convert.ToInt32(this.assigned_to.SelectedItem.Value));

            set_msg(Util.CapitalizeFirstLetter(ApplicationSettings.SingularBugLabel) + " was updated.");

            this.comment.Value = string.Empty;

            set_controls_field_permission(PermissionLevel, security);

            if (bugFieldsHaveChanged)
            {
                // Fetch again from database
                var updatedBug = Bug.GetBugDataRow(this.Id, security, this.DsCustomCols);

                // Allow for customization not written by me
                Workflow.CustomAdjustControls(updatedBug, security.User, this);
            }

            load_user_dropdown(security);
        }

        public void load_other_dropdowns_and_select_defaults(DataSet dsDefaults, ISecurity security)
        {
            // org
            string defaultValue;

            defaultValue = Convert.ToString(Security.User.Org);
            foreach (ListItem li in this.org.Items)
                if (li.Value == defaultValue)
                    li.Selected = true;
                else
                    li.Selected = false;

            // category
            if (dsDefaults.Tables[1].Rows.Count > 0)
                defaultValue = Convert.ToString((int) dsDefaults.Tables[1].Rows[0][0]);
            else
                defaultValue = "0";

            foreach (ListItem li in this.category.Items)
                if (li.Value == defaultValue)
                    li.Selected = true;
                else
                    li.Selected = false;

            // priority
            if (dsDefaults.Tables[2].Rows.Count > 0)
                defaultValue = Convert.ToString((int) dsDefaults.Tables[2].Rows[0][0]);
            else
                defaultValue = "0";
            foreach (ListItem li in this.priority.Items)
                if (li.Value == defaultValue)
                    li.Selected = true;
                else
                    li.Selected = false;

            // status
            if (dsDefaults.Tables[3].Rows.Count > 0)
                defaultValue = Convert.ToString((int) dsDefaults.Tables[3].Rows[0][0]);
            else
                defaultValue = "0";
            foreach (ListItem li in this.status.Items)
                if (li.Value == defaultValue)
                    li.Selected = true;
                else
                    li.Selected = false;

            // udf
            if (dsDefaults.Tables[4].Rows.Count > 0)
                defaultValue = Convert.ToString((int) dsDefaults.Tables[4].Rows[0][0]);
            else
                defaultValue = "0";
            foreach (ListItem li in this.udf.Items)
                if (li.Value == defaultValue)
                    li.Selected = true;
                else
                    li.Selected = false;
        }

        public void load_project_and_user_dropdown_for_insert(DataTable projectDefault, ISecurity security)
        {
            // get default values
            var initialProject = (string) Session["project"];

            // project
            if (Security.User.ForcedProject != 0)
                initialProject = Convert.ToString(Security.User.ForcedProject);

            if (initialProject != null && initialProject != "0")
            {
                foreach (ListItem li in this.project.Items)
                    if (li.Value == initialProject)
                        li.Selected = true;
                    else
                        li.Selected = false;
            }
            else
            {
                string defaultValue;
                if (projectDefault.Rows.Count > 0)
                    defaultValue = Convert.ToString((int) projectDefault.Rows[0][0]);
                else
                    defaultValue = "0";

                foreach (ListItem li in this.project.Items)
                    if (li.Value == defaultValue)
                        li.Selected = true;
                    else
                        li.Selected = false;
            }

            load_user_dropdown(security);
        }

        public void load_project_and_user_dropdown_for_update(ISecurity security)
        {
            // Project
            if (this.prev_project.Value != "0")
            {
                // see if already in the dropdown.
                var alreadyInDropdown = false;
                foreach (ListItem li in this.project.Items)
                    if (li.Value == this.prev_project.Value)
                    {
                        alreadyInDropdown = true;
                        break;
                    }

                // Add to the list, even if permissions don't allow it now, because, in the past, they did allow it.
                if (!alreadyInDropdown)
                    this.project.Items.Add(
                        new ListItem(this.prev_project_name.Value, this.prev_project.Value));
            }

            foreach (ListItem li in this.project.Items)
                if (li.Value == this.prev_project.Value)
                    li.Selected = true;
                else
                    li.Selected = false;

            load_user_dropdown(security);
        }

        public void load_user_dropdown(ISecurity security)
        {
            // What's selected now?   Save it before we refresh the dropdown.
            var currentValue = string.Empty;

            if (IsPostBack) currentValue = this.assigned_to.SelectedItem.Value;

            // Load the user dropdown, which changes per project
            // Only users explicitly allowed will be listed
            if (ApplicationSettings.DefaultPermissionLevel == 0)
                Sql = @"
/* users this project */ select us_id, case when $fullnames then us_lastname + ', ' + us_firstname else us_username end us_username
from users
inner join orgs on us_org = og_id
where us_active = 1
and og_can_be_assigned_to = 1
and ($og_other_orgs_permission_level <> 0 or $og_id = og_id or (og_external_user = 0 and $og_can_assign_to_internal_users))
and us_id in
    (select pu_user from project_user_xref
        where pu_project = $pj
        and pu_permission_level <> 0)
order by us_username; ";
            // Only users explictly DISallowed will be omitted
            else
                Sql = @"
/* users this project */ select us_id, case when $fullnames then us_lastname + ', ' + us_firstname else us_username end us_username
from users
inner join orgs on us_org = og_id
where us_active = 1
and og_can_be_assigned_to = 1
and ($og_other_orgs_permission_level <> 0 or $og_id = og_id or (og_external_user = 0 and $og_can_assign_to_internal_users))
and us_id not in
    (select pu_user from project_user_xref
        where pu_project = $pj
        and pu_permission_level = 0)
order by us_username; ";

            if (!ApplicationSettings.UseFullNames)
                // false condition
                Sql = Sql.Replace("$fullnames", "0 = 1");
            else
                // true condition
                Sql = Sql.Replace("$fullnames", "1 = 1");

            if (this.project.SelectedItem != null)
                Sql = Sql.Replace("$pj", this.project.SelectedItem.Value);
            else
                Sql = Sql.Replace("$pj", "0");

            Sql = Sql.Replace("$og_id", Convert.ToString(Security.User.Org));
            Sql = Sql.Replace("$og_other_orgs_permission_level",
                Convert.ToString((int)Security.User.OtherOrgsPermissionLevel));

            if (Security.User.CanAssignToInternalUsers)
                Sql = Sql.Replace("$og_can_assign_to_internal_users", "1 = 1");
            else
                Sql = Sql.Replace("$og_can_assign_to_internal_users", "0 = 1");

            DtUsers = DbUtil.GetDataSet(Sql).Tables[0];

            this.assigned_to.DataSource = new DataView(DtUsers);
            this.assigned_to.DataTextField = "us_username";
            this.assigned_to.DataValueField = "us_id";
            this.assigned_to.DataBind();
            this.assigned_to.Items.Insert(0, new ListItem("[not assigned]", "0"));

            // It can happen that the user in the db is not listed in the dropdown, because of a subsequent change in permissions.
            // Since that user IS the user associated with the bug, let's force it into the dropdown.
            if (this.Id != 0) // if existing bug
                if (this.prev_assigned_to.Value != "0")
                {
                    // see if already in the dropdown.
                    var userInDropdown = false;
                    foreach (ListItem li in this.assigned_to.Items)
                        if (li.Value == this.prev_assigned_to.Value)
                        {
                            userInDropdown = true;
                            break;
                        }

                    // Add to the list, even if permissions don't allow it now, because, in the past, they did allow it.
                    if (!userInDropdown)
                        this.assigned_to.Items.Insert(1,
                            new ListItem(this.prev_assigned_to_username.Value, this.prev_assigned_to.Value));
                }

            // At this point, all the users we need are in the dropdown.
            // Now selected the selected.
            if (string.IsNullOrEmpty(currentValue)) currentValue = this.prev_assigned_to.Value;

            // Select the user.  We are either restoring the previous selection
            // or selecting what was in the database.
            if (currentValue != "0")
                foreach (ListItem li in this.assigned_to.Items)
                    if (li.Value == currentValue)
                        li.Selected = true;
                    else
                        li.Selected = false;

            // if nothing else is selected. select the default user for the project
            if (this.assigned_to.SelectedItem.Value == "0")
            {
                var projectDefaultUser = 0;
                if (this.project.SelectedItem != null)
                {
                    // get the default user of the project
                    projectDefaultUser = Util.GetDefaultUser(Convert.ToInt32(this.project.SelectedItem.Value));

                    if (projectDefaultUser != 0)
                        foreach (ListItem li in this.assigned_to.Items)
                            if (Convert.ToInt32(li.Value) == projectDefaultUser)
                                li.Selected = true;
                            else
                                li.Selected = false;
                }
            }
        }

        public static string get_custom_col_default_value(object o)
        {
            var defaultval = Convert.ToString(o);

            // populate the sql default value of a custom field
            if (defaultval.Length > 2)
                if (defaultval[0] == '('
                    && defaultval[defaultval.Length - 1] == ')')
                {
                    var defaultvalSql = "select " + defaultval.Substring(1, defaultval.Length - 2);
                    defaultval = Convert.ToString(DbUtil.ExecuteScalar(defaultvalSql));
                }

            return defaultval;
        }

        public void format_subcribe_cancel_link(ISecurity security)
        {
            var notificationEmailEnabled = ApplicationSettings.NotificationEmailEnabled;
            if (notificationEmailEnabled)
            {
                int subscribed;
                if (!IsPostBack)
                {
                    subscribed = (int) this.DrBug["subscribed"];
                }
                else
                {
                    // User might have changed bug to a project where we automatically subscribe
                    // so be prepared to format the link even if this isn't the first time in.
                    Sql = "select count(1) from bug_subscriptions where bs_bug = $bg and bs_user = $us";
                    Sql = Sql.Replace("$bg", Convert.ToString(this.Id));
                    Sql = Sql.Replace("$us", Convert.ToString(Security.User.Usid));
                    subscribed = (int) DbUtil.ExecuteScalar(Sql);
                }

                if (Security.User.IsGuest) // wouldn't make sense to share an email address
                {
                    this.subscriptions.InnerHtml = string.Empty;
                }
                else
                {
                    var subscriptionLink =
                        "<a id='notifications' title='Get or stop getting email notifications about changes to this item.'"
                        + " href='javascript:toggle_notifications("
                        + Convert.ToString(this.Id)
                        + ")'><img src=" + ResolveUrl("~/Content/images/telephone.png") + " border=0 align=top>&nbsp;<span id='get_stop_notifications'>";

                    if (subscribed > 0)
                        subscriptionLink += "stop notifications</span></a>";
                    else
                        subscriptionLink += "get notifications</span></a>";

                    this.subscriptions.InnerHtml = subscriptionLink;
                }
            }
        }

        public void set_org_field_permission(SecurityPermissionLevel bugPermissionLevel, ISecurity security)
        {
            // pick the most restrictive permission
            var permLevel = bugPermissionLevel < Security.User.OrgFieldPermissionLevel
                ? bugPermissionLevel
                : Security.User.OrgFieldPermissionLevel;

            if (permLevel == SecurityPermissionLevel.PermissionNone)
            {
                this.org_label.Visible = false;
                this.org.Visible = false;
                this.prev_org.Visible = false;
            }
            else if (permLevel == SecurityPermissionLevel.PermissionReadonly)
            {
                this.org.Visible = false;
                this.static_org.InnerText = this.org.SelectedItem.Text;
            }
            else // editable
            {
                this.static_org.Visible = false;
            }
        }

        public void set_shortdesc_field_permission()
        {
            // turn on the spans to hold the data
            if (this.Id != 0)
            {
                this.static_short_desc.Style["display"] = "block";
                this.short_desc.Visible = false;
            }

            this.static_short_desc.InnerText = this.short_desc.Value;
        }

        public void set_tags_field_permission(SecurityPermissionLevel bugPermissionLevel, ISecurity security)
        {
            /// JUNK testing using cat permission
            // pick the most restrictive permission
            var permLevel = bugPermissionLevel < Security.User.TagsFieldPermissionLevel
                ? bugPermissionLevel
                : Security.User.TagsFieldPermissionLevel;

            if (permLevel == SecurityPermissionLevel.PermissionNone)
            {
                this.static_tags.Visible = false;
                this.tags_label.Visible = false;
                this.tags.Visible = false;
                this.tags_link.Visible = false;
                this.prev_tags.Visible = false;
                //tags_row.Style.display = "none";
            }
            else if (permLevel == SecurityPermissionLevel.PermissionReadonly)
            {
                if (this.Id != 0)
                {
                    this.tags.Visible = false;
                    this.tags_link.Visible = false;
                    this.static_tags.Visible = true;
                    this.static_tags.InnerText = this.tags.Value;
                }
                else
                {
                    this.tags_label.Visible = false;
                    this.tags.Visible = false;
                    this.tags_link.Visible = false;
                }
            }
            else // editable
            {
                this.static_tags.Visible = false;
            }
        }

        public void set_category_field_permission(SecurityPermissionLevel bugPermissionLevel, ISecurity security)
        {
            // pick the most restrictive permission
            var permLevel = bugPermissionLevel < Security.User.CategoryFieldPermissionLevel
                ? bugPermissionLevel
                : Security.User.CategoryFieldPermissionLevel;

            if (permLevel == SecurityPermissionLevel.PermissionNone)
            {
                this.category_label.Visible = false;
                this.category.Visible = false;
                this.prev_category.Visible = false;
            }
            else if (permLevel == SecurityPermissionLevel.PermissionReadonly)
            {
                this.category.Visible = false;
                this.static_category.InnerText = this.category.SelectedItem.Text;
            }
            else // editable
            {
                this.static_category.Visible = false;
            }
        }

        public void set_priority_field_permission(SecurityPermissionLevel bugPermissionLevel, ISecurity security)
        {
            // pick the most restrictive permission
            var permLevel = bugPermissionLevel < Security.User.PriorityFieldPermissionLevel
                ? bugPermissionLevel
                : Security.User.PriorityFieldPermissionLevel;

            if (permLevel == SecurityPermissionLevel.PermissionNone)
            {
                this.priority_label.Visible = false;
                this.priority.Visible = false;
                this.prev_priority.Visible = false;
            }
            else if (permLevel == SecurityPermissionLevel.PermissionReadonly)
            {
                this.priority.Visible = false;
                this.static_priority.InnerText = this.priority.SelectedItem.Text;
            }
            else // editable
            {
                this.static_priority.Visible = false;
            }
        }

        public void set_status_field_permission(SecurityPermissionLevel bugPermissionLevel, ISecurity security)
        {
            // pick the most restrictive permission
            var permLevel = bugPermissionLevel < Security.User.StatusFieldPermissionLevel
                ? bugPermissionLevel
                : Security.User.StatusFieldPermissionLevel;

            if (permLevel == SecurityPermissionLevel.PermissionNone)
            {
                this.status_label.Visible = false;
                this.status.Visible = false;
                this.prev_status.Visible = false;
            }
            else if (permLevel == SecurityPermissionLevel.PermissionReadonly)
            {
                this.status.Visible = false;
                this.static_status.InnerText = this.status.SelectedItem.Text;
            }
            else // editable
            {
                this.static_status.Visible = false;
            }
        }

        public void set_project_field_permission(SecurityPermissionLevel bugPermissionLevel, ISecurity security)
        {
            var permLevel = bugPermissionLevel < Security.User.ProjectFieldPermissionLevel
                ? bugPermissionLevel
                : Security.User.ProjectFieldPermissionLevel;

            if (permLevel == SecurityPermissionLevel.PermissionNone)
            {
                this.project_label.Visible = false;
                this.project.Visible = false;
                this.prev_project.Visible = false;
            }
            else if (permLevel == SecurityPermissionLevel.PermissionReadonly)
            {
                this.project.Visible = false;
                this.static_project.InnerText = this.project.SelectedItem.Text;
            }
            else
            {
                this.static_project.Visible = false;
            }
        }

        public void set_assigned_field_permission(SecurityPermissionLevel bugPermissionLevel, ISecurity security)
        {
            var permLevel = bugPermissionLevel < Security.User.AssignedToFieldPermissionLevel
                ? bugPermissionLevel
                : Security.User.AssignedToFieldPermissionLevel;

            if (permLevel == SecurityPermissionLevel.PermissionNone)
            {
                this.assigned_to_label.Visible = false;
                this.assigned_to.Visible = false;
                this.prev_assigned_to.Visible = false;
            }
            else if (permLevel == SecurityPermissionLevel.PermissionReadonly)
            {
                this.assigned_to.Visible = false;
                this.static_assigned_to.InnerText = this.assigned_to.SelectedItem.Text;
            }
        }

        public  void set_udf_field_permission(SecurityPermissionLevel bugPermissionLevel, ISecurity security)
        {
            // pick the most restrictive permission
            var permLevel = bugPermissionLevel < Security.User.UdfFieldPermissionLevel
                ? bugPermissionLevel
                : Security.User.UdfFieldPermissionLevel;

            if (permLevel == SecurityPermissionLevel.PermissionNone)
            {
                this.udf_label.Visible = false;
                this.udf.Visible = false;
                this.prev_udf.Visible = false;
            }
            else if (permLevel == SecurityPermissionLevel.PermissionReadonly)
            {
                this.udf.Visible = false;
                this.static_udf.InnerText = this.udf.SelectedItem.Text;
            }
            else // editable
            {
                this.static_udf.Visible = false;
            }
        }

        public void set_controls_field_permission(SecurityPermissionLevel bugPermissionLevel, ISecurity security)
        {
            if (bugPermissionLevel == SecurityPermissionLevel.PermissionReadonly
                || bugPermissionLevel == SecurityPermissionLevel.PermissionReporter)
            {
                // even turn off commenting updating for read only
                if (PermissionLevel == SecurityPermissionLevel.PermissionReadonly)
                {
                    this.submit_button.Disabled = true;
                    this.submit_button.Visible = false;
                    if (ApplicationSettings.DisplayAnotherButtonInEditBugPage)
                    {
                        this.submit_button2.Disabled = true;
                        this.submit_button2.Visible = false;
                    }

                    this.comment_label.Visible = false;
                    this.comment.Visible = false;
                }

                set_project_field_permission(SecurityPermissionLevel.PermissionReadonly, security);
                set_org_field_permission(SecurityPermissionLevel.PermissionReadonly, security);
                set_category_field_permission(SecurityPermissionLevel.PermissionReadonly, security);
                set_tags_field_permission(SecurityPermissionLevel.PermissionReadonly, security);
                set_priority_field_permission(SecurityPermissionLevel.PermissionReadonly, security);
                set_status_field_permission(SecurityPermissionLevel.PermissionReadonly, security);
                set_assigned_field_permission(SecurityPermissionLevel.PermissionReadonly, security);
                set_udf_field_permission(SecurityPermissionLevel.PermissionReadonly, security);
                set_shortdesc_field_permission();

                this.internal_only_label.Visible = false;
                this.internal_only.Visible = false;
            }
            else
            {
                // Call these functions so that the field level permissions can kick in
                if (Security.User.ForcedProject != 0)
                    set_project_field_permission(SecurityPermissionLevel.PermissionReadonly, security);
                else
                    set_project_field_permission(SecurityPermissionLevel.PermissionAll, security);

                if (Security.User.OtherOrgsPermissionLevel == 0)
                    set_org_field_permission(SecurityPermissionLevel.PermissionReadonly, security);
                else
                    set_org_field_permission(SecurityPermissionLevel.PermissionAll, security);
                set_category_field_permission(SecurityPermissionLevel.PermissionAll, security);
                set_tags_field_permission(SecurityPermissionLevel.PermissionAll, security);
                set_priority_field_permission(SecurityPermissionLevel.PermissionAll, security);
                set_status_field_permission(SecurityPermissionLevel.PermissionAll, security);
                set_assigned_field_permission(SecurityPermissionLevel.PermissionAll, security);
                set_udf_field_permission(SecurityPermissionLevel.PermissionAll, security);
            }
        }

        public void format_prev_next_bug()
        {
            // for next/prev bug links
            var dvBugs = (DataView) Session["bugs"];

            if (dvBugs != null)
            {
                var prevBug = 0;
                var nextBug = 0;
                var thisBugFound = false;

                // read through the list of bugs looking for the one that matches this one
                var positionInList = 0;
                var savePositionInList = 0;
                foreach (DataRowView drv in dvBugs)
                {
                    positionInList++;
                    if (thisBugFound)
                    {
                        // step 3 - get the next bug - we're done
                        nextBug = (int) drv[1];
                        break;
                    }

                    if (this.Id == (int) drv[1])
                    {
                        // step 2 - we found this - set switch
                        savePositionInList = positionInList;
                        thisBugFound = true;
                    }
                    else
                    {
                        // step 1 - save the previous just in case the next one IS this bug
                        prevBug = (int) drv[1];
                    }
                }

                var prevNextLink = string.Empty;

                if (thisBugFound)
                {
                    if (prevBug != 0)
                        prevNextLink =
                            "&nbsp;&nbsp;&nbsp;&nbsp;<a class=warn href=" + ResolveUrl("~/Bugs/Edit.aspx") + "?id="
                            + Convert.ToString(prevBug)
                            + "><img src=" + ResolveUrl("~/Content/images/arrow_up.png") + " border=0 align=top>prev</a>";
                    else
                        prevNextLink = "&nbsp;&nbsp;&nbsp;&nbsp;<span class=gray_link>prev</span>";

                    if (nextBug != 0)
                        prevNextLink +=
                            "&nbsp;&nbsp;&nbsp;&nbsp;<a class=warn href=" + ResolveUrl("~/Bugs/Edit.aspx") + "?id="
                            + Convert.ToString(nextBug)
                            + ">next<img src=" + ResolveUrl("~/Content/images/arrow_down.png") + " border=0 align=top></a>";
                    else
                        prevNextLink += "&nbsp;&nbsp;&nbsp;&nbsp;<span class=gray_link>next</span>";

                    prevNextLink += "&nbsp;&nbsp;&nbsp;<span class=smallnote>"
                                      + Convert.ToString(savePositionInList)
                                      + " of "
                                      + Convert.ToString(dvBugs.Count)
                                      + "</span>";

                    this.prev_next.InnerHtml = prevNextLink;
                }
            }
        }

        public void load_dropdowns(User user, ISecurity security)
        {
            // only show projects where user has permissions
            // 0
            Sql = @"/* drop downs */ select pj_id, pj_name
        from projects
        left outer join project_user_xref on pj_id = pu_project
        and pu_user = $us
        where pj_active = 1
        and isnull(pu_permission_level,$dpl) not in (0, 1)
        order by pj_name;";

            Sql = Sql.Replace("$us", Convert.ToString(Security.User.Usid));
            Sql = Sql.Replace("$dpl", ApplicationSettings.DefaultPermissionLevel.ToString());

            // 1
            Sql += "\nselect og_id, og_name from orgs where og_active = 1 order by og_name;";

            // 2
            Sql += "\nselect ct_id, ct_name from categories order by ct_sort_seq, ct_name;";

            // 3
            Sql += "\nselect pr_id, pr_name from priorities order by pr_sort_seq, pr_name;";

            // 4
            Sql += "\nselect st_id, st_name from statuses order by st_sort_seq, st_name;";

            // 5
            Sql += "\nselect udf_id, udf_name from user_defined_attribute order by udf_sort_seq, udf_name;";

            // do a batch of sql statements
            var dsDropdowns = DbUtil.GetDataSet(Sql);

            this.project.DataSource = dsDropdowns.Tables[0];
            this.project.DataTextField = "pj_name";
            this.project.DataValueField = "pj_id";
            this.project.DataBind();

            if (ApplicationSettings.DefaultPermissionLevel == 2) this.project.Items.Insert(0, new ListItem("[no project]", "0"));

            this.org.DataSource = dsDropdowns.Tables[1];
            this.org.DataTextField = "og_name";
            this.org.DataValueField = "og_id";
            this.org.DataBind();
            this.org.Items.Insert(0, new ListItem("[no organization]", "0"));

            this.category.DataSource = dsDropdowns.Tables[2];
            this.category.DataTextField = "ct_name";
            this.category.DataValueField = "ct_id";
            this.category.DataBind();
            this.category.Items.Insert(0, new ListItem("[no category]", "0"));

            this.priority.DataSource = dsDropdowns.Tables[3];
            this.priority.DataTextField = "pr_name";
            this.priority.DataValueField = "pr_id";
            this.priority.DataBind();
            this.priority.Items.Insert(0, new ListItem("[no priority]", "0"));

            this.status.DataSource = dsDropdowns.Tables[4];
            this.status.DataTextField = "st_name";
            this.status.DataValueField = "st_id";
            this.status.DataBind();
            this.status.Items.Insert(0, new ListItem("[no status]", "0"));

            this.udf.DataSource = dsDropdowns.Tables[5];
            this.udf.DataTextField = "udf_name";
            this.udf.DataValueField = "udf_id";
            this.udf.DataBind();
            this.udf.Items.Insert(0, new ListItem("[none]", "0"));
        }

        public static string get_dropdown_text_from_value(DropDownList dropdown, string value)
        {
            foreach (ListItem li in dropdown.Items)
                if (li.Value == value)
                    return li.Text;

            return dropdown.Items[0].Text;
        }

        public bool did_something_change()
        {
            var somethingChanged = false;

            if (this.prev_short_desc.Value != this.short_desc.Value
                || this.prev_tags.Value != this.tags.Value
                || this.comment.Value.Length > 0
                || this.clone_ignore_bugid.Value == "1"
                || this.prev_project.Value != this.project.SelectedItem.Value
                || this.prev_org.Value != this.org.SelectedItem.Value
                || this.prev_category.Value != this.category.SelectedItem.Value
                || this.prev_priority.Value != this.priority.SelectedItem.Value
                || this.prev_assigned_to.Value != this.assigned_to.SelectedItem.Value
                || this.prev_status.Value != this.status.SelectedItem.Value
                || ApplicationSettings.ShowUserDefinedBugAttribute && this.prev_udf.Value != this.udf.SelectedItem.Value)
            {
                this.clone_ignore_bugid.Value = "0";
                somethingChanged = true;
            }

            // Now look to see if custom fields changed
            if (!somethingChanged)
                foreach (var columnName in this.HashCustomCols.Keys)
                {
                    var after = this.HashCustomCols[columnName];
                    if (after == null) continue; // because there's no control, nothing for user to edit

                    var before = Util.FormatDbValue(this.DrBug[columnName]);

                    if (before != after.Trim())
                    {
                        somethingChanged = true;
                        break;
                    }
                }

            if (!somethingChanged)
                if (Request["pcd1"] != null && this.prev_pcd1.Value != Request["pcd1"]
                    || Request["pcd2"] != null && this.prev_pcd2.Value != Request["pcd2"]
                    || Request["pcd3"] != null && this.prev_pcd3.Value != Request["pcd3"])
                    somethingChanged = true;

            return somethingChanged;
        }

        // returns true if there was a change
        public bool record_changes(ISecurity security)
        {
            var baseSql = @"
        insert into bug_posts
        (bp_bug, bp_user, bp_date, bp_comment, bp_type)
        values($id, $us, getdate(), N'$update_msg', 'update')";

            baseSql = baseSql.Replace("$id", Convert.ToString(this.Id));
            baseSql = baseSql.Replace("$us", Convert.ToString(Security.User.Usid));

            string from;
            Sql = string.Empty;

            var doUpdate = false;

            if (this.prev_short_desc.Value != this.short_desc.Value)
            {
                doUpdate = true;
                Sql += baseSql.Replace(
                    "$update_msg",
                    "changed desc from \""
                    + this.prev_short_desc.Value.Replace("'", "''") + "\" to \""
                    + this.short_desc.Value.Replace("'", "''") + "\"");

                this.prev_short_desc.Value = this.short_desc.Value;
            }

            if (this.prev_tags.Value != this.tags.Value)
            {
                doUpdate = true;
                Sql += baseSql.Replace(
                    "$update_msg",
                    "changed tags from \""
                    + this.prev_tags.Value.Replace("'", "''") + "\" to \""
                    + this.tags.Value.Replace("'", "''") + "\"");

                this.prev_tags.Value = this.tags.Value;

                if (ApplicationSettings.EnableTags)
                {
                    Tags.BuildTagIndex(Application);
                }
            }

            if (this.project.SelectedItem.Value != this.prev_project.Value)
            {
                // The "from" might not be in the dropdown anymore
                //from = get_dropdown_text_from_value(project, prev_project.Value);

                doUpdate = true;
                Sql += baseSql.Replace(
                    "$update_msg",
                    "changed project from \""
                    + this.prev_project_name.Value.Replace("'", "''") + "\" to \""
                    + this.project.SelectedItem.Text.Replace("'", "''") + "\"");

                this.prev_project.Value = this.project.SelectedItem.Value;
                this.prev_project_name.Value = this.project.SelectedItem.Text;
            }

            if (this.prev_org.Value != this.org.SelectedItem.Value)
            {
                from = get_dropdown_text_from_value(this.org, this.prev_org.Value);

                doUpdate = true;
                Sql += baseSql.Replace(
                    "$update_msg",
                    "changed organization from \""
                    + from.Replace("'", "''") + "\" to \""
                    + this.org.SelectedItem.Text.Replace("'", "''") + "\"");

                this.prev_org.Value = this.org.SelectedItem.Value;
            }

            if (this.prev_category.Value != this.category.SelectedItem.Value)
            {
                from = get_dropdown_text_from_value(this.category, this.prev_category.Value);

                doUpdate = true;
                Sql += baseSql.Replace(
                    "$update_msg",
                    "changed category from \""
                    + from.Replace("'", "''") + "\" to \""
                    + this.category.SelectedItem.Text.Replace("'", "''") + "\"");

                this.prev_category.Value = this.category.SelectedItem.Value;
            }

            if (this.prev_priority.Value != this.priority.SelectedItem.Value)
            {
                from = get_dropdown_text_from_value(this.priority, this.prev_priority.Value);

                doUpdate = true;
                Sql += baseSql.Replace(
                    "$update_msg",
                    "changed priority from \""
                    + from.Replace("'", "''") + "\" to \""
                    + this.priority.SelectedItem.Text.Replace("'", "''") + "\"");

                this.prev_priority.Value = this.priority.SelectedItem.Value;
            }

            if (this.prev_assigned_to.Value != this.assigned_to.SelectedItem.Value)
            {
                this.AssignedToChanged = true; // for notifications

                // The "from" might not be in the dropdown anymore...
                //from = get_dropdown_text_from_value(assigned_to, prev_assigned_to.Value);

                doUpdate = true;
                Sql += baseSql.Replace(
                    "$update_msg",
                    "changed assigned_to from \""
                    + this.prev_assigned_to_username.Value.Replace("'", "''") + "\" to \""
                    + this.assigned_to.SelectedItem.Text.Replace("'", "''") + "\"");

                this.prev_assigned_to.Value = this.assigned_to.SelectedItem.Value;
                this.prev_assigned_to_username.Value = this.assigned_to.SelectedItem.Text;
            }

            if (this.prev_status.Value != this.status.SelectedItem.Value)
            {
                this.StatusChanged = true; // for notifications

                from = get_dropdown_text_from_value(this.status, this.prev_status.Value);

                doUpdate = true;
                Sql += baseSql.Replace(
                    "$update_msg",
                    "changed status from \""
                    + from.Replace("'", "''") + "\" to \""
                    + this.status.SelectedItem.Text.Replace("'", "''") + "\"");

                this.prev_status.Value = this.status.SelectedItem.Value;
            }

            if (ApplicationSettings.ShowUserDefinedBugAttribute)
                if (this.prev_udf.Value != this.udf.SelectedItem.Value)
                {
                    from = get_dropdown_text_from_value(this.udf, this.prev_udf.Value);

                    doUpdate = true;
                    Sql += baseSql.Replace(
                        "$update_msg",
                        "changed " + ApplicationSettings.UserDefinedBugAttributeName
                                   + " from \""
                                   + from.Replace("'", "''") + "\" to \""
                                   + this.udf.SelectedItem.Text.Replace("'", "''") + "\"");

                    this.prev_udf.Value = this.udf.SelectedItem.Value;
                }

            // Record changes in custom columns

            foreach (DataRow drcc in this.DsCustomCols.Tables[0].Rows)
            {
                var columnName = (string) drcc["name"];

                if (Security.User.DictCustomFieldPermissionLevel[columnName] !=
                    SecurityPermissionLevel.PermissionAll) continue;

                var before = Util.FormatDbValue(this.DrBug[columnName]);
                var after = this.HashCustomCols[columnName];

                if (before == "0") before = string.Empty;

                if (after == "0") after = string.Empty;

                if (before.Trim() != after.Trim())
                {
                    if ((string) drcc["dropdown type"] == "users")
                    {
                        var sqlGetUsername = string.Empty;
                        if (string.IsNullOrEmpty(before))
                        {
                            before = string.Empty;
                        }
                        else
                        {
                            sqlGetUsername = "select us_username from users where us_id = $1";
                            before = (string) DbUtil.ExecuteScalar(sqlGetUsername.Replace("$1",
                                Util.SanitizeInteger(before)));
                        }

                        if (string.IsNullOrEmpty(after))
                        {
                            after = string.Empty;
                        }
                        else
                        {
                            sqlGetUsername = "select us_username from users where us_id = $1";
                            after = (string) DbUtil.ExecuteScalar(sqlGetUsername.Replace("$1",
                                Util.SanitizeInteger(after)));
                        }
                    }

                    doUpdate = true;
                    Sql += baseSql.Replace(
                        "$update_msg",
                        "changed " + columnName + " from \"" + before.Trim().Replace("'", "''") + "\" to \"" +
                        after.Trim().Replace("'", "''") + "\"");
                }
            }

            // Handle project custom dropdowns
            if (Request["label_pcd1"] != null && Request["pcd1"] != null && this.prev_pcd1.Value != Request["pcd1"])
            {
                doUpdate = true;
                Sql += baseSql.Replace(
                    "$update_msg",
                    "changed "
                    + Request["label_pcd1"].Replace("'", "''")
                    + " from \"" + this.prev_pcd1.Value + "\" to \"" + Request["pcd1"].Replace("'", "''") + "\"");

                this.prev_pcd1.Value = Request["pcd1"];
            }

            if (Request["label_pcd2"] != null && Request["pcd2"] != null && this.prev_pcd2.Value != Request["pcd2"].Replace("'", "''"))
            {
                doUpdate = true;
                Sql += baseSql.Replace(
                    "$update_msg",
                    "changed "
                    + Request["label_pcd2"].Replace("'", "''")
                    + " from \"" + this.prev_pcd2.Value + "\" to \"" + Request["pcd2"].Replace("'", "''") + "\"");

                this.prev_pcd2.Value = Request["pcd2"];
            }

            if (Request["label_pcd3"] != null && Request["pcd3"] != null && this.prev_pcd3.Value != Request["pcd3"])
            {
                doUpdate = true;
                Sql += baseSql.Replace(
                    "$update_msg",
                    "changed "
                    + Request["label_pcd3"].Replace("'", "''")
                    + " from \"" + this.prev_pcd3.Value + "\" to \"" + Request["pcd3"].Replace("'", "''") + "\"");

                this.prev_pcd3.Value = Request["pcd3"];
            }

            if (doUpdate && ApplicationSettings.TrackBugHistory) // you might not want the debris to grow
            {
                DbUtil.ExecuteNonQuery(Sql);
            }

            if (this.project.SelectedItem.Value != this.prev_project.Value)
                PermissionLevel = fetch_permission_level(this.project.SelectedItem.Value, security);

            // return true if something did change
            return doUpdate;
        }

        public SecurityPermissionLevel fetch_permission_level(string projectToCheck, ISecurity security)
        {
            // fetch the revised permission level
            Sql = @"declare @permission_level int
        set @permission_level = -1
        select @permission_level = isnull(pu_permission_level,$dpl)
        from project_user_xref
        where pu_project = $pj
        and pu_user = $us
        if @permission_level = -1 set @permission_level = $dpl
        select @permission_level";

            Sql = Sql.Replace("$dpl", ApplicationSettings.DefaultPermissionLevel.ToString());
            Sql = Sql.Replace("$pj", projectToCheck);
            Sql = Sql.Replace("$us", Convert.ToString(Security.User.Usid));
            var pl = (int) DbUtil.ExecuteScalar(Sql);

            // reduce permissions for guest
            //if (Security.User.is_guest && permission_level == Security.PERMISSION_ALL)
            //{
            //	pl = Security.PERMISSION_REPORTER;
            //}

            return (SecurityPermissionLevel)pl;
        }

        public bool validate(ISecurity security)
        {
            var good = true;
            this.custom_validation_err_msg.InnerText = string.Empty;

            if (string.IsNullOrEmpty(this.short_desc.Value))
            {
                good = false;
                this.short_desc_err.InnerText = "Short Description is required.";
            }
            else
            {
                this.short_desc_err.InnerText = string.Empty;
            }

            if (!did_something_change()) return false;

            // validate custom columns
            foreach (DataRow drcc in this.DsCustomCols.Tables[0].Rows)
            {
                var name = (string) drcc["name"];

                if (Security.User.DictCustomFieldPermissionLevel[name] != SecurityPermissionLevel.PermissionAll) continue;

                var val = Request[name];

                if (val == null) continue;

                val = val.Replace("'", "''");

                // if a date was entered, convert to db format
                if (val.Length > 0)
                {
                    var datatype = drcc["datatype"].ToString();

                    if (datatype == "datetime")
                    {
                        try
                        {
                            DateTime.Parse(val, Util.GetCultureInfo());
                        }
                        catch (FormatException)
                        {
                            append_custom_field_msg("\"" + name + "\" not in a valid date format.<br>");
                            good = false;
                        }
                    }
                    else if (datatype == "int")
                    {
                        if (!Util.IsInt(val))
                        {
                            append_custom_field_msg("\"" + name + "\" must be an integer.<br>");
                            good = false;
                        }
                    }
                    else if (datatype == "decimal")
                    {
                        var xprec = Convert.ToInt32(drcc["xprec"]);
                        var xscale = Convert.ToInt32(drcc["xscale"]);

                        var decimalError = Util.IsValidDecimal(name, val, xprec - xscale, xscale);
                        if (!string.IsNullOrEmpty(decimalError))
                        {
                            append_custom_field_msg(decimalError + "<br>");
                            good = false;
                        }
                    }
                }
                else
                {
                    var nullable = (int) drcc["isnullable"];
                    if (nullable == 0)
                    {
                        append_custom_field_msg("\"" + name + "\" is required.<br>");
                        good = false;
                    }
                }
            }

            // validate assigned to user versus 

            if (!does_assigned_to_have_permission_for_org(
                Convert.ToInt32(this.assigned_to.SelectedValue),
                Convert.ToInt32(this.org.SelectedValue)))
            {
                this.assigned_to_err.InnerText = "User does not have permission for the Organization";
                good = false;
            }
            else
            {
                this.assigned_to_err.InnerText = string.Empty;
            }

            // custom validations go here
            if (!Workflow.CustomValidations(this.DrBug, security.User,
                this, this.custom_validation_err_msg))
                good = false;

            return good;
        }

        public static bool does_assigned_to_have_permission_for_org(int assignedTo, int org)
        {
            if (assignedTo < 1) return true;

            var sql = @"
/* validate org versus assigned_to */
select case when og_other_orgs_permission_level <> 0
or $bg_org = og_id then 1
else 0 end as [answer]
from users
inner join orgs on us_org = og_id
where us_id = @us_id";

            sql = sql.Replace("@us_id", Convert.ToString(assignedTo));
            sql = sql.Replace("$bg_org", Convert.ToString(org));

            var allowed = DbUtil.ExecuteScalar(sql);

            if (allowed != null && Convert.ToInt32(allowed) == 1)
                return true;
            return false;
        }

        public void set_msg(string s)
        {
            this.msg.InnerHtml = s;
            if (ApplicationSettings.DisplayAnotherButtonInEditBugPage)
            {
                this.msg2.InnerHtml = s;
            }
        }

        private void set_custom_field_msg(string s)
        {
            this.custom_field_msg.InnerHtml = s;
            if (ApplicationSettings.DisplayAnotherButtonInEditBugPage)
            {
                this.custom_field_msg2.InnerHtml = s;
            }
        }

        public void append_custom_field_msg(string s)
        {
            this.custom_field_msg.InnerHtml += s;
            if (ApplicationSettings.DisplayAnotherButtonInEditBugPage)
            {
                this.custom_field_msg2.InnerHtml += s;
            }
        }

        public void display_custom_fields(ISecurity security)
        {
            var minTextAreaSize = ApplicationSettings.TextAreaThreshold;
            var maxTextAreaRows = ApplicationSettings.MaxTextAreaRows;

            // Create the custom column INPUT elements
            foreach (DataRow drcc in this.DsCustomCols.Tables[0].Rows)
            {
                var columnName = (string) drcc["name"];

                var fieldPermissionLevel = Security.User.DictCustomFieldPermissionLevel[columnName];
                if (fieldPermissionLevel == SecurityPermissionLevel.PermissionNone) continue;

                var fieldId = columnName.Replace(" ", "");

                Response.Write("\n<tr id=\"" + fieldId + "_row\">");
                Response.Write("<td nowrap><span id=\"" + fieldId + "_label\">");
                Response.Write(columnName);

                var permissionOnOriginal = PermissionLevel;

                if (!string.IsNullOrEmpty(this.prev_project.Value)
                    && (this.project.SelectedItem == null || this.project.SelectedItem.Value != this.prev_project.Value))
                    permissionOnOriginal = fetch_permission_level(this.prev_project.Value, security);

                if (permissionOnOriginal == SecurityPermissionLevel.PermissionReadonly
                    || permissionOnOriginal == SecurityPermissionLevel.PermissionReporter)
                    Response.Write(":</span><td align=left width=600px>");
                else
                    Response.Write(":</span><td align=left>");

                //20040413 WWR - If a custom database field is over the defined character length, use a TextArea control
                var fieldLength = int.Parse(drcc["length"].ToString());
                var datatype = drcc["datatype"].ToString();

                var dropdownType = Convert.ToString(drcc["dropdown type"]);

                if (permissionOnOriginal == SecurityPermissionLevel.PermissionReadonly
                    || fieldPermissionLevel == SecurityPermissionLevel.PermissionReadonly)
                {
                    string text;

                    if (this.Id == 0) // add
                    {
                        text = get_custom_col_default_value(drcc["default value"]);
                    }
                    else
                    {
                        text = Convert.ToString(this.DrBug[columnName]);

                        if (datatype == "datetime") text = Util.FormatDbDateTime(text);
                    }

                    if (fieldLength > minTextAreaSize && !string.IsNullOrEmpty(text))
                    {
                        // more readable if there is a lot of text
                        Response.Write("<div class='short_desc_static'  id=\"" + fieldId + "_static\"><pre>");
                        Response.Write(HttpUtility.HtmlEncode(text));
                        Response.Write("</pre></div>");
                    }
                    else
                    {
                        Response.Write("<span class='stat' id=\"" + fieldId + "_static\">");
                        if (dropdownType == "users")
                        {
                            if (!string.IsNullOrEmpty(text))
                            {
                                var viewOnlyUserId = Convert.ToInt32(text);
                                var dvUsers = new DataView(DtUsers);
                                foreach (DataRowView drv in dvUsers)
                                    if (viewOnlyUserId == (int) drv[0])
                                    {
                                        Response.Write(Convert.ToString(drv[1]));
                                        break;
                                    }
                            }
                        }
                        else
                        {
                            Response.Write(HttpUtility.HtmlEncode(text));
                        }

                        Response.Write("</span>");
                    }
                }
                else
                {
                    if (fieldLength > minTextAreaSize
                        && dropdownType != "normal"
                        && dropdownType != "users")
                    {
                        Response.Write("<textarea class='txt resizable'");
                        Response.Write(" onkeydown=\"return count_chars('" + fieldId + "'," + fieldLength + ")\" ");
                        Response.Write(" onkeyup=\"return count_chars('" + fieldId + "'," + fieldLength + ")\" ");
                        Response.Write(" cols=\"" + minTextAreaSize + "\" rows=\"" +
                                       (fieldLength / minTextAreaSize > maxTextAreaRows
                                           ? maxTextAreaRows
                                           : fieldLength / minTextAreaSize) + "\" ");
                        Response.Write(" name=\"" + columnName + "\"");
                        Response.Write(" id=\"" + fieldId + "\" >");
                        Response.Write(HttpUtility.HtmlEncode(this.HashCustomCols[columnName]));
                        Response.Write("</textarea><div class=smallnote id=\"" + fieldId + "_cnt\">&nbsp;</div>");
                    }
                    else
                    {
                        var dropdownVals = Convert.ToString(drcc["vals"]);

                        if (!string.IsNullOrEmpty(dropdownType) || !string.IsNullOrEmpty(dropdownVals))
                        {
                            var selectedValue = this.HashCustomCols[columnName].Trim();

                            Response.Write("<select ");

                            Response.Write(" id=\"" + fieldId + "\"");
                            Response.Write(" name=\"" + columnName + "\"");
                            Response.Write(">");

                            if (dropdownType != "users")
                            {
                                var options = Util.SplitDropdownVals(dropdownVals);
                                var decodedSelectedValue = HttpUtility.HtmlDecode(selectedValue);
                                for (var j = 0; j < options.Length; j++)
                                {
                                    Response.Write("<option");
                                    var decodedOption = HttpUtility.HtmlDecode(options[j]);
                                    if (decodedOption == decodedSelectedValue) Response.Write(" selected ");
                                    Response.Write(">");
                                    Response.Write(decodedOption);
                                    Response.Write("</option>");
                                }
                            }
                            else
                            {
                                Response.Write("<option value=0>[not selected]</option>");

                                var dvUsers = new DataView(DtUsers);
                                foreach (DataRowView drv in dvUsers)
                                {
                                    var userId = Convert.ToString(drv[0]);
                                    var userName = Convert.ToString(drv[1]);

                                    Response.Write("<option value=");
                                    Response.Write(userId);

                                    if (userId == selectedValue) Response.Write(" selected ");
                                    Response.Write(">");
                                    Response.Write(userName);
                                    Response.Write("</option>");
                                }
                            }

                            Response.Write("</select>");
                        }
                        else
                        {
                            Response.Write("<input type=text onkeydown=\"mark_dirty()\" onkeyup=\"mark_dirty()\" ");

                            // match the size of the text field to the size of the database field

                            if (datatype.IndexOf("char") > -1)
                            {
                                Response.Write(" size=" + Convert.ToString(fieldLength));
                                Response.Write(" maxlength=" + Convert.ToString(fieldLength));
                            }

                            Response.Write(" name=\"" + columnName + "\"");
                            Response.Write(" id=\"" + fieldId + "\"");
                            Response.Write(" value=\"");
                            Response.Write(this.HashCustomCols[columnName].Replace("\"", "&quot;"));

                            if (datatype == "datetime")
                            {
                                Response.Write("\" class='txt date'  >");
                                Response.Write("<a style=\"font-size: 8pt;\"href=\"javascript:show_calendar('"
                                               + fieldId
                                               + "');\">[select]</a>");
                            }
                            else
                            {
                                Response.Write("\" class='txt' >");
                            }
                        }
                    }
                } // end if readonly or editable
            } // end loop through custom fields
        }

        public void display_project_specific_custom_fields(ISecurity security)
        {
            // create project custom dropdowns
            if (this.project.SelectedItem != null
                && this.project.SelectedItem.Value != null
                && this.project.SelectedItem.Value != "0")
            {
                Sql = @"select
            isnull(pj_enable_custom_dropdown1,0) [pj_enable_custom_dropdown1],
            isnull(pj_enable_custom_dropdown2,0) [pj_enable_custom_dropdown2],
            isnull(pj_enable_custom_dropdown3,0) [pj_enable_custom_dropdown3],
            isnull(pj_custom_dropdown_label1,'') [pj_custom_dropdown_label1],
            isnull(pj_custom_dropdown_label2,'') [pj_custom_dropdown_label2],
            isnull(pj_custom_dropdown_label3,'') [pj_custom_dropdown_label3],
            isnull(pj_custom_dropdown_values1,'') [pj_custom_dropdown_values1],
            isnull(pj_custom_dropdown_values2,'') [pj_custom_dropdown_values2],
            isnull(pj_custom_dropdown_values3,'') [pj_custom_dropdown_values3]
            from projects where pj_id = $pj";

                Sql = Sql.Replace("$pj", this.project.SelectedItem.Value);

                var projectDr = DbUtil.GetDataRow(Sql);

                if (projectDr != null)
                    for (var i = 1; i < 4; i++)
                        if ((int) projectDr["pj_enable_custom_dropdown" + Convert.ToString(i)] == 1)
                        {
                            // GC: 20-Feb-08: Modified to add an ID to each custom row for CSS customisation
                            Response.Write("\n<tr id=\"pcdrow" + Convert.ToString(i) + "\"><td nowrap>");

                            Response.Write("<span id=label_pcd" + Convert.ToString(i) + ">");
                            Response.Write(projectDr["pj_custom_dropdown_label" + Convert.ToString(i)]);
                            Response.Write("</span>");
                            // End GC
                            Response.Write("<td nowrap>");

                            var permissionOnOriginal = PermissionLevel;
                            if (!string.IsNullOrEmpty(this.prev_project.Value) && this.project.SelectedItem.Value != this.prev_project.Value)
                                permissionOnOriginal = fetch_permission_level(this.prev_project.Value, security);

                            if (permissionOnOriginal == SecurityPermissionLevel.PermissionReadonly
                                || permissionOnOriginal == SecurityPermissionLevel.PermissionReporter)
                            {
                                // GC: 20-Feb-08: Modified to add an ID to the SPAN as well for easier CSS customisation
                                //Response.Write ("<span class="stat">");
                                Response.Write("<span class='stat' id=span_pcd" + Convert.ToString(i) + ">");

                                if (IsPostBack)
                                {
                                    var val = HttpUtility.HtmlEncode(Request["pcd" + Convert.ToString(i)]);

                                    Response.Write(val);
                                    Response.Write("</span>");

                                    Response.Write("<input type=hidden name=pcd"
                                                   + Convert.ToString(i)
                                                   + " value=\""
                                                   + val
                                                   + "\">");
                                }
                                else
                                {
                                    if (this.Id != 0)
                                    {
                                        var val = (string) this.DrBug[
                                            "bg_project_custom_dropdown_value" + Convert.ToString(i)];
                                        Response.Write(val);
                                        Response.Write("</span>");

                                        Response.Write("<input type=hidden name=pcd"
                                                       + Convert.ToString(i)
                                                       + " value=\""
                                                       + val
                                                       + "\">");
                                    }
                                    else
                                    {
                                        Response.Write("</span>");
                                    }
                                }
                            }
                            else
                            {
                                // create a hidden area to carry the label

                                Response.Write("<input type=hidden");
                                Response.Write(" name=label_pcd" + Convert.ToString(i));
                                Response.Write(" value=\"");
                                Response.Write(projectDr["pj_custom_dropdown_label" + Convert.ToString(i)]);
                                Response.Write("\">");

                                // create a dropdown

                                Response.Write("<select");
                                // GC: 20-Feb-08: Added an ID as well for easier CSS customisation
                                Response.Write(" name=pcd" + Convert.ToString(i));
                                Response.Write(" id=pcd" + Convert.ToString(i) + ">");
                                var options = Util.SplitDropdownVals(
                                    (string) projectDr["pj_custom_dropdown_values" + Convert.ToString(i)]);

                                var selectedValue = string.Empty;

                                if (IsPostBack)
                                {
                                    selectedValue = Request["pcd" + Convert.ToString(i)];
                                }
                                else
                                {
                                    // first time viewing existing
                                    if (this.Id != 0)
                                        selectedValue =
                                            (string) this.DrBug[
                                                "bg_project_custom_dropdown_value" + Convert.ToString(i)];
                                }

                                for (var j = 0; j < options.Length; j++)
                                {
                                    Response.Write("<option value=\"" + options[j] + "\"");

                                    //if (options[j] == selected_value)
                                    if (HttpUtility.HtmlDecode(options[j]) == selectedValue)
                                        Response.Write(" selected ");
                                    Response.Write(">");
                                    Response.Write(options[j]);
                                }

                                Response.Write("</select>");
                            }
                        }
            }
        }

        public void display_bug_relationships(ISecurity security)
        {
            DsPosts = PrintBug.GetBugPosts(this.Id, Security.User.ExternalUser, this.HistoryInline);
            var linkMarker = ApplicationSettings.BugLinkMarker;
            var reLinkMarker = new Regex(linkMarker + "([0-9]+)");
            var dictLinkedBugs = new SortedDictionary<int, int>();

            // fish out bug links
            foreach (DataRow drPost in DsPosts.Tables[0].Rows)
                if ((string) drPost["bp_type"] == "comment")
                {
                    var matchCollection = reLinkMarker.Matches((string) drPost["bp_comment"]);

                    foreach (Match match in matchCollection)
                    {
                        var otherBugid = Convert.ToInt32(match.Groups[1].ToString());
                        if (otherBugid != this.Id) dictLinkedBugs[otherBugid] = 1;
                    }
                }

            if (dictLinkedBugs.Count > 0)
            {
                Response.Write("Linked to:");
                foreach (var intOtherBugid in dictLinkedBugs.Keys)
                {
                    var stringOtherBugid = Convert.ToString(intOtherBugid);

                    Response.Write("&nbsp;<a href=" + ResolveUrl("~/Bugs/Edit.aspx") + "?id=");
                    Response.Write(stringOtherBugid);
                    Response.Write(">");
                    Response.Write(stringOtherBugid);
                    Response.Write("</a>");
                }
            }
        }
    }
}
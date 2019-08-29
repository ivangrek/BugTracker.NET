/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text.RegularExpressions;
    using System.Web;
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using Core;

    public partial class edit_bug : Page
    {
        public bool assigned_to_changed;

        public string comment_formated;
        public string comment_search;
        public string commentType;
        public DataRow dr_bug;

        public DataSet ds_custom_cols;
        public DataSet ds_posts;
        public DataTable dt_users;

        public bool good = true;
        public SortedDictionary<string, string> hash_custom_cols = new SortedDictionary<string, string>();
        public bool history_inline;
        public int id;

        public bool images_inline = true;

        public int permission_level;

        public Security security;
        public string sql;

        public bool status_changed;

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            set_msg("");
            set_custom_field_msg("");

            var string_bugid = Request["id"];
            if (string_bugid == null || string_bugid == "0" ||
                string_bugid != "0" && this.clone_ignore_bugid.Value == "1")
            {
                // New
                this.id = 0;
                this.bugid_label.InnerHtml = "Description:&nbsp;";
            }
            else
            {
                if (!Util.is_int(string_bugid))
                {
                    display_bugid_must_be_integer();
                    return;
                }

                // Existing
                this.id = Convert.ToInt32(string_bugid);

                this.bugid_label.Visible = true;
                this.bugid_label.InnerHtml = Util.capitalize_first_letter(Util.get_setting("SingularBugLabel", "bug")) +
                                             " ID:&nbsp;";
            }

            // Get list of custom fields

            this.ds_custom_cols = Util.get_custom_columns();

            if (!IsPostBack)
            {
                // Fetch stuff from db and put on page

                if (this.id == 0)
                {
                    prepare_for_insert();
                }
                else
                {
                    get_cookie_values_for_show_hide_toggles();

                    // Get this entry's data from the db and fill in the form
                    this.dr_bug = Bug.get_bug_datarow(this.id, this.security, this.ds_custom_cols);

                    prepare_for_update();
                }

                if (this.security.user.external_user || Util.get_setting("EnableInternalOnlyPosts", "0") == "0")
                {
                    this.internal_only.Visible = false;
                    this.internal_only_label.Visible = false;
                }
            }
            else
            {
                get_cookie_values_for_show_hide_toggles();

                this.dr_bug = Bug.get_bug_datarow(this.id, this.security, this.ds_custom_cols);

                load_incoming_custom_col_vals_into_hash();

                if (did_user_hit_submit_button()) // or is this a project dropdown autopostback?
                {
                    this.good = validate();

                    if (this.good)
                    {
                        // Actually do the update
                        if (this.id == 0)
                            do_insert();
                        else
                            do_update();
                    }
                    else // bad, invalid
                    {
                        // Say we didn't do anything.
                        if (this.id == 0)
                            set_msg(Util.capitalize_first_letter(Util.get_setting("SingularBugLabel", "bug")) +
                                    " was not created.");
                        else
                            set_msg(Util.capitalize_first_letter(Util.get_setting("SingularBugLabel", "bug")) +
                                    " was not updated.");
                        load_user_dropdown();
                    }
                }
                else
                {
                    // This is the project dropdown autopost back.
                    load_user_dropdown();
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
                this.images_inline = false;
            else
                this.images_inline = true;

            cookie = Request.Cookies["history_inline"];
            if (cookie == null || cookie.Value == "0")
                this.history_inline = false;
            else
                this.history_inline = true;
        }

        public void prepare_for_insert()
        {
            if (this.security.user.adds_not_allowed)
            {
                Util.display_bug_not_found(Response, this.security, this.id); // TODO wrong message
                return;
            }

            Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - Create ";
            Page.Title += Util.capitalize_first_letter(Util.get_setting("SingularBugLabel", "bug"));

            this.submit_button.Value = "Create";

            if (Util.get_setting("DisplayAnotherButtonInEditBugPage", "0") == "1") this.submit_button2.Value = "Create";

            load_dropdowns_for_insert();

            // Prepare for custom columns
            foreach (DataRow drcc in this.ds_custom_cols.Tables[0].Rows)
            {
                var column_name = (string) drcc["name"];
                if (this.security.user.dict_custom_field_permission_level[column_name] != Security.PERMISSION_NONE)
                {
                    var defaultval = get_custom_col_default_value(drcc["default value"]);
                    this.hash_custom_cols.Add(column_name, defaultval);
                }
            }

            // We don't know the project yet, so all permissions
            set_controls_field_permission(Security.PERMISSION_ALL);

            // Execute code not written by me
            Workflow.custom_adjust_controls(null, this.security.user, this);
        }

        public void load_dropdowns_for_insert()
        {
            load_dropdowns(this.security.user);

            // Get the defaults
            this.sql = "\nselect top 1 pj_id from projects where pj_default = 1 order by pj_name;"; // 0
            this.sql += "\nselect top 1 ct_id from categories where ct_default = 1 order by ct_name;"; // 1
            this.sql += "\nselect top 1 pr_id from priorities where pr_default = 1 order by pr_name;"; // 2
            this.sql += "\nselect top 1 st_id from statuses where st_default = 1 order by st_name;"; // 3
            this.sql +=
                "\nselect top 1 udf_id from user_defined_attribute where udf_default = 1 order by udf_name;"; // 4

            var ds_defaults = DbUtil.get_dataset(this.sql);

            load_project_and_user_dropdown_for_insert(ds_defaults.Tables[0]);

            load_other_dropdowns_and_select_defaults(ds_defaults);
        }

        public void prepare_for_update()
        {
            if (this.dr_bug == null)
            {
                Util.display_bug_not_found(Response, this.security, this.id);
                return;
            }

            // look at permission level and react accordingly
            this.permission_level = (int) this.dr_bug["pu_permission_level"];

            if (this.permission_level == Security.PERMISSION_NONE)
            {
                Util.display_you_dont_have_permission(Response, this.security);
                return;
            }

            foreach (DataRow drcc in this.ds_custom_cols.Tables[0].Rows)
            {
                var column_name = (string) drcc["name"];
                if (this.security.user.dict_custom_field_permission_level[column_name] != Security.PERMISSION_NONE)
                {
                    var val = Util.format_db_value(this.dr_bug[column_name]);
                    this.hash_custom_cols.Add(column_name, val);
                }
            }

            // move stuff to the page

            this.bugid.InnerText = Convert.ToString((int) this.dr_bug["id"]);

            // Fill in this form
            this.short_desc.Value = (string) this.dr_bug["short_desc"];
            this.tags.Value = (string) this.dr_bug["bg_tags"];
            Page.Title = Util.capitalize_first_letter(Util.get_setting("SingularBugLabel", "bug"))
                         + " ID " + Convert.ToString(this.dr_bug["id"]) + " " + (string) this.dr_bug["short_desc"];

            // reported by
            string s;
            s = "Created by ";
            s += PrintBug.format_email_username(
                true,
                Convert.ToInt32(this.dr_bug["id"]), this.permission_level,
                Convert.ToString(this.dr_bug["reporter_email"]),
                Convert.ToString(this.dr_bug["reporter"]),
                Convert.ToString(this.dr_bug["reporter_fullname"]));
            s += " on ";
            s += Util.format_db_date_and_time(this.dr_bug["reported_date"]);
            s += ", ";
            s += Util.how_long_ago((int) this.dr_bug["seconds_ago"]);

            this.reported_by.InnerHtml = s;

            // save current values in previous, so that later we can write the audit trail when things change
            this.prev_short_desc.Value = (string) this.dr_bug["short_desc"];
            this.prev_tags.Value = (string) this.dr_bug["bg_tags"];
            this.prev_project.Value = Convert.ToString((int) this.dr_bug["project"]);
            this.prev_project_name.Value = Convert.ToString(this.dr_bug["current_project"]);
            this.prev_org.Value = Convert.ToString((int) this.dr_bug["organization"]);
            this.prev_org_name.Value = Convert.ToString(this.dr_bug["og_name"]);
            this.prev_category.Value = Convert.ToString((int) this.dr_bug["category"]);
            this.prev_priority.Value = Convert.ToString((int) this.dr_bug["priority"]);
            this.prev_assigned_to.Value = Convert.ToString((int) this.dr_bug["assigned_to_user"]);
            this.prev_assigned_to_username.Value = Convert.ToString(this.dr_bug["assigned_to_username"]);
            this.prev_status.Value = Convert.ToString((int) this.dr_bug["status"]);
            this.prev_udf.Value = Convert.ToString((int) this.dr_bug["udf"]);
            this.prev_pcd1.Value = (string) this.dr_bug["bg_project_custom_dropdown_value1"];
            this.prev_pcd2.Value = (string) this.dr_bug["bg_project_custom_dropdown_value2"];
            this.prev_pcd3.Value = (string) this.dr_bug["bg_project_custom_dropdown_value3"];

            load_dropdowns_for_update();

            load_project_and_user_dropdown_for_update(); // must come before set_controls_field_permission, after assigning to prev_ values

            set_controls_field_permission(this.permission_level);

            this.snapshot_timestamp.Value = Convert.ToDateTime(this.dr_bug["snapshot_timestamp"])
                .ToString("yyyyMMdd HH\\:mm\\:ss\\:fff");

            prepare_a_bunch_of_links_for_update();

            format_prev_next_bug();

            // save for next bug
            if (this.project.SelectedItem != null) Session["project"] = this.project.SelectedItem.Value;

            // Execute code not written by me
            Workflow.custom_adjust_controls(this.dr_bug, this.security.user, this);
        }

        public void prepare_a_bunch_of_links_for_update()
        {
            var toggle_images_link = "<a href='javascript:toggle_images2("
                                     + Convert.ToString(this.id) + ")'><span id=hideshow_images>"
                                     + (this.images_inline ? "hide" : "show")
                                     + " inline images"
                                     + "</span></a>";
            this.toggle_images.InnerHtml = toggle_images_link;

            var toggle_history_link = "<a href='javascript:toggle_history2("
                                      + Convert.ToString(this.id) + ")'><span id=hideshow_history>"
                                      + (this.history_inline ? "hide" : "show")
                                      + " change history"
                                      + "</span></a>";
            this.toggle_history.InnerHtml = toggle_history_link;

            if (this.permission_level == Security.PERMISSION_ALL)
            {
                var clone_link = "<a class=warn href=\"javascript:clone()\" "
                                 + " title='Create a copy of this item'><img src=paste_plain.png border=0 align=top>&nbsp;create copy</a>";
                this.clone.InnerHtml = clone_link;
            }

            if (this.permission_level != Security.PERMISSION_READONLY)
            {
                var attachment_link =
                    "<img src=attach.gif align=top>&nbsp;<a href=\"javascript:open_popup_window('add_attachment.aspx','add attachment ',"
                    + Convert.ToString(this.id)
                    + ",600,300)\" title='Attach an image, document, or other file to this item'>add attachment</a>";
                this.attachment.InnerHtml = attachment_link;
            }
            else
            {
                this.attachment.Visible = false;
            }

            if (!this.security.user.is_guest)
            {
                if (this.permission_level != Security.PERMISSION_READONLY)
                {
                    var send_email_link = "<a href='javascript:send_email("
                                          + Convert.ToString(this.id)
                                          + ")' title='Send an email about this item'><img src=email_edit.png border=0 align=top>&nbsp;send email</a>";
                    this.send_email.InnerHtml = send_email_link;
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

            if (this.permission_level != Security.PERMISSION_READONLY)
            {
                var subscribers_link = "<a target=_blank href=view_subscribers.aspx?id="
                                       + Convert.ToString(this.id)
                                       + " title='View users who have subscribed to email notifications for this item'><img src=telephone_edit.png border=0 align=top>&nbsp;subscribers</a>";
                this.subscribers.InnerHtml = subscribers_link;
            }
            else
            {
                this.subscribers.Visible = false;
            }

            if (Util.get_setting("EnableRelationships", "0") == "1")
            {
                var relationship_cnt = 0;
                if (this.id != 0) relationship_cnt = (int) this.dr_bug["relationship_cnt"];
                var relationships_link = "<a target=_blank href=relationships.aspx?bgid="
                                         + Convert.ToString(this.id)
                                         + " title='Create a relationship between this item and another item'><img src=database_link.png border=0 align=top>&nbsp;relationships(<span id=relationship_cnt>" +
                                         relationship_cnt + "</span>)</a>";
                this.relationships.InnerHtml = relationships_link;
            }
            else
            {
                this.relationships.Visible = false;
            }

            if (Util.get_setting("EnableSubversionIntegration", "0") == "1")
            {
                var revision_cnt = 0;
                if (this.id != 0) revision_cnt = (int) this.dr_bug["svn_revision_cnt"];
                var svn_revisions_link = "<a target=_blank href=svn_view_revisions.aspx?id="
                                         + Convert.ToString(this.id)
                                         + " title='View Subversion svn_revisions related to this item'><img src=svn.png border=0 align=top>&nbsp;svn revisions(" +
                                         revision_cnt + ")</a>";
                this.svn_revisions.InnerHtml = svn_revisions_link;
            }
            else
            {
                this.svn_revisions.Visible = false;
            }

            if (Util.get_setting("EnableGitIntegration", "0") == "1")
            {
                var revision_cnt = 0;
                if (this.id != 0) revision_cnt = (int) this.dr_bug["git_commit_cnt"];
                var git_commits_link = "<a target=_blank href=git_view_revisions.aspx?id="
                                       + Convert.ToString(this.id)
                                       + " title='View git git_commits related to this item'><img src=git.png border=0 align=top>&nbsp;git commits(" +
                                       revision_cnt + ")</a>";
                this.git_commits.InnerHtml = git_commits_link;
            }
            else
            {
                this.git_commits.Visible = false;
            }

            if (Util.get_setting("EnableMercurialIntegration", "0") == "1")
            {
                var revision_cnt = 0;
                if (this.id != 0) revision_cnt = (int) this.dr_bug["hg_commit_cnt"];
                var hg_revisions_link = "<a target=_blank href=hg_view_revisions.aspx?id="
                                        + Convert.ToString(this.id)
                                        + " title='View mercurial git_hg_revisions related to this item'><img src=hg.png border=0 align=top>&nbsp;hg revisions(" +
                                        revision_cnt + ")</a>";
                this.hg_revisions.InnerHtml = hg_revisions_link;
            }
            else
            {
                this.hg_revisions.Visible = false;
            }

            if (this.security.user.is_admin || this.security.user.can_view_tasks)
            {
                if (Util.get_setting("EnableTasks", "0") == "1")
                {
                    var task_cnt = 0;
                    if (this.id != 0) task_cnt = (int) this.dr_bug["task_cnt"];
                    var tasks_link = "<a target=_blank href=tasks_frame.aspx?bugid="
                                     + Convert.ToString(this.id)
                                     + " title='View sub-tasks/time-tracking entries related to this item'><img src=clock.png border=0 align=top>&nbsp;tasks/time(<span id=task_cnt>" +
                                     task_cnt + "</span>)</a>";
                    this.tasks.InnerHtml = tasks_link;
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

            format_subcribe_cancel_link();

            this.print.InnerHtml = "<a target=_blank href=print_bug.aspx?id="
                                   + Convert.ToString(this.id)
                                   + " title='Display this item in a printer-friendly format'><img src=printer.png border=0 align=top>&nbsp;print</a>";

            // merge
            if (!this.security.user.is_guest)
            {
                if (this.security.user.is_admin
                    || this.security.user.can_merge_bugs)
                {
                    var merge_bug_link = "<a href=merge_bug.aspx?id="
                                         + Convert.ToString(this.id)
                                         + " title='Merge this item and another item together'><img src=database_refresh.png border=0 align=top>&nbsp;merge</a>";

                    this.merge_bug.InnerHtml = merge_bug_link;
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
            if (!this.security.user.is_guest)
            {
                if (this.security.user.is_admin
                    || this.security.user.can_delete_bug)
                {
                    var delete_bug_link = "<a href=delete_bug.aspx?id="
                                          + Convert.ToString(this.id)
                                          + " title='Delete this item'><img src=delete.png border=0 align=top>&nbsp;delete</a>";

                    this.delete_bug.InnerHtml = delete_bug_link;
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
            if (Util.get_setting("CustomBugLinkLabel", "") != "")
            {
                var custom_bug_link = "<a href="
                                      + Util.get_setting("CustomBugLinkUrl", "")
                                      + "?bugid="
                                      + Convert.ToString(this.id)
                                      + "><img src=brick.png border=0 align=top>&nbsp;"
                                      + Util.get_setting("CustomBugLinkLabel", "")
                                      + "</a>";

                this.custom.InnerHtml = custom_bug_link;
            }
            else
            {
                this.custom.Visible = false;
            }
        }

        public void load_dropdowns_for_update()
        {
            load_dropdowns(this.security.user);

            // select the dropdowns

            foreach (ListItem li in this.category.Items)
                if (Convert.ToInt32(li.Value) == (int) this.dr_bug["category"])
                    li.Selected = true;
                else
                    li.Selected = false;

            foreach (ListItem li in this.priority.Items)
                if (Convert.ToInt32(li.Value) == (int) this.dr_bug["priority"])
                    li.Selected = true;
                else
                    li.Selected = false;

            foreach (ListItem li in this.status.Items)
                if (Convert.ToInt32(li.Value) == (int) this.dr_bug["status"])
                    li.Selected = true;
                else
                    li.Selected = false;

            foreach (ListItem li in this.udf.Items)
                if (Convert.ToInt32(li.Value) == (int) this.dr_bug["udf"])
                    li.Selected = true;
                else
                    li.Selected = false;

            // special logic for org
            if (this.id != 0)
            {
                // Org
                if (this.prev_org.Value != "0")
                {
                    var already_in_dropdown = false;
                    foreach (ListItem li in this.org.Items)
                        if (li.Value == this.prev_org.Value)
                        {
                            already_in_dropdown = true;
                            break;
                        }

                    // Add to the list, even if permissions don't allow it now, because, in the past, they did allow it.
                    if (!already_in_dropdown)
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

        public void display_bugid_must_be_integer()
        {
            // Display an error because the bugid must be an integer

            Response.Write("<link rel=StyleSheet href=btnet.css type=text/css>");
            this.security.write_menu(Response, Util.get_setting("PluralBugLabel", "bugs"));
            Response.Write("<p>&nbsp;</p><div class=align>");
            Response.Write("<div class=err>Error: ");
            Response.Write(Util.capitalize_first_letter(Util.get_setting("SingularBugLabel", "bug")));
            Response.Write(" ID must be an integer.</div>");
            Response.Write("<p><a href=bugs.aspx>View ");
            Response.Write(Util.get_setting("PluralBugLabel", "bugs"));
            Response.Write("</a>");
            Response.End();
        }

        public void get_comment_text_from_control()
        {
            if (this.security.user.use_fckeditor)
            {
                this.comment_formated = Util.strip_dangerous_tags(this.comment.Value);
                this.comment_search = Util.strip_html(this.comment.Value);
                this.commentType = "text/html";
            }
            else
            {
                this.comment_formated = HttpUtility.HtmlDecode(this.comment.Value);
                this.comment_search = this.comment_formated;
                this.commentType = "text/plain";
            }
        }

        public void load_incoming_custom_col_vals_into_hash()
        {
            // Fetch the values of the custom columns from the Request and stash them in a hash table.

            foreach (DataRow drcc in this.ds_custom_cols.Tables[0].Rows)
            {
                var column_name = (string) drcc["name"];

                if (this.security.user.dict_custom_field_permission_level[column_name] != Security.PERMISSION_NONE)
                    this.hash_custom_cols.Add(column_name, Request[column_name]);
            }
        }

        public void do_insert()
        {
            get_comment_text_from_control();

            // Project specific
            var pcd1 = Request["pcd1"];
            var pcd2 = Request["pcd2"];
            var pcd3 = Request["pcd3"];

            if (pcd1 == null) pcd1 = "";
            if (pcd2 == null) pcd2 = "";
            if (pcd3 == null) pcd3 = "";

            pcd1 = pcd1.Replace("'", "''");
            pcd2 = pcd2.Replace("'", "''");
            pcd3 = pcd3.Replace("'", "''");

            var new_ids = Bug.insert_bug(this.short_desc.Value, this.security, this.tags.Value,
                Convert.ToInt32(this.project.SelectedItem.Value),
                Convert.ToInt32(this.org.SelectedItem.Value),
                Convert.ToInt32(this.category.SelectedItem.Value),
                Convert.ToInt32(this.priority.SelectedItem.Value),
                Convert.ToInt32(this.status.SelectedItem.Value),
                Convert.ToInt32(this.assigned_to.SelectedItem.Value),
                Convert.ToInt32(this.udf.SelectedItem.Value),
                pcd1,
                pcd2,
                pcd3, this.comment_formated, this.comment_search,
                null, // from
                null, // cc
                this.commentType, this.internal_only.Checked, this.hash_custom_cols,
                true); // send notifications

            if (this.tags.Value != "" && Util.get_setting("EnableTags", "0") == "1") Tags.build_tag_index(Application);

            this.id = new_ids.bugid;

            WhatsNew.add_news(this.id, this.short_desc.Value, "added", this.security);

            this.new_id.Value = Convert.ToString(this.id);
            set_msg(Util.capitalize_first_letter(Util.get_setting("SingularBugLabel", "bug")) + " was created.");

            // save for next bug
            Session["project"] = this.project.SelectedItem.Value;

            Response.Redirect("edit_bug.aspx?id=" + Convert.ToString(this.id));
        }

        public void do_update()
        {
            this.permission_level = fetch_permission_level(this.project.SelectedItem.Value);

            //if (project.SelectedItem.Value == prev_project.Value)
            //{
            //    set_controls_field_permission(permission_level);
            //}

            var bug_fields_have_changed = false;
            var bugpost_fields_have_changed = false;

            get_comment_text_from_control();

            string new_project;
            if (this.project.SelectedItem.Value != this.prev_project.Value)
            {
                new_project = this.project.SelectedItem.Value;
                var permission_level_on_new_project = fetch_permission_level(new_project);
                if (Security.PERMISSION_NONE == permission_level_on_new_project
                    || Security.PERMISSION_READONLY == permission_level_on_new_project)
                {
                    set_msg(Util.capitalize_first_letter(Util.get_setting("SingularBugLabel", "bug"))
                            + " was not updated. You do not have the necessary permissions to change this "
                            + Util.get_setting("SingularBugLabel", "bug") + " to the specified Project.");
                    return;
                }

                this.permission_level = permission_level_on_new_project;
            }
            else
            {
                new_project = Util.sanitize_integer(this.prev_project.Value);
            }

            this.sql = @"declare @now datetime
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

            this.sql = this.sql.Replace("$sd", this.short_desc.Value.Replace("'", "''"));
            this.sql = this.sql.Replace("$tags", this.tags.Value.Replace("'", "''"));
            this.sql = this.sql.Replace("$lu", Convert.ToString(this.security.user.usid));
            this.sql = this.sql.Replace("$id", Convert.ToString(this.id));
            this.sql = this.sql.Replace("$pj", new_project);
            this.sql = this.sql.Replace("$og", this.org.SelectedItem.Value);
            this.sql = this.sql.Replace("$ct", this.category.SelectedItem.Value);
            this.sql = this.sql.Replace("$pr", this.priority.SelectedItem.Value);
            this.sql = this.sql.Replace("$au", this.assigned_to.SelectedItem.Value);
            this.sql = this.sql.Replace("$st", this.status.SelectedItem.Value);
            this.sql = this.sql.Replace("$udf", this.udf.SelectedItem.Value);
            this.sql = this.sql.Replace("$snapshot_datetime", this.snapshot_timestamp.Value);

            if (this.permission_level == Security.PERMISSION_READONLY
                || this.permission_level == Security.PERMISSION_REPORTER)
            {
                this.sql = this.sql.Replace("$pcd_placeholder", "");
            }
            else
            {
                this.sql = this.sql.Replace("$pcd_placeholder", @",
bg_project_custom_dropdown_value1 = N'$pcd1',
bg_project_custom_dropdown_value2 = N'$pcd2',
bg_project_custom_dropdown_value3 = N'$pcd3'
");

                var pcd1 = Request["pcd1"];
                var pcd2 = Request["pcd2"];
                var pcd3 = Request["pcd3"];

                if (pcd1 == null) pcd1 = "";
                if (pcd2 == null) pcd2 = "";
                if (pcd3 == null) pcd3 = "";

                this.sql = this.sql.Replace("$pcd1", pcd1.Replace("'", "''"));
                this.sql = this.sql.Replace("$pcd2", pcd2.Replace("'", "''"));
                this.sql = this.sql.Replace("$pcd3", pcd3.Replace("'", "''"));
            }

            if (this.ds_custom_cols.Tables[0].Rows.Count == 0 || this.permission_level != Security.PERMISSION_ALL)
            {
                this.sql = this.sql.Replace("$custom_cols_placeholder", "");
            }
            else
            {
                var custom_cols_sql = "";

                foreach (DataRow drcc in this.ds_custom_cols.Tables[0].Rows)
                {
                    var column_name = (string) drcc["name"];

                    // if we've made customizations that cause the field to not come back to us,
                    // don't replace something with null
                    var o = Request[column_name];
                    if (o == null) continue;

                    // skip if no permission to update
                    if (this.security.user.dict_custom_field_permission_level[column_name] !=
                        Security.PERMISSION_ALL) continue;

                    custom_cols_sql += ",[" + column_name + "]";
                    custom_cols_sql += " = ";

                    var datatype = (string) drcc["datatype"];

                    var custom_col_val = Util.request_to_string_for_sql(
                        Request[column_name],
                        datatype);

                    custom_cols_sql += custom_col_val;
                }

                this.sql = this.sql.Replace("$custom_cols_placeholder", custom_cols_sql);
            }

            var last_update_date = (DateTime) DbUtil.execute_scalar(this.sql);

            WhatsNew.add_news(this.id, this.short_desc.Value, "updated", this.security);

            var date_from_db = last_update_date.ToString("yyyyMMdd HH\\:mm\\:ss\\:fff");
            var date_from_webpage = this.snapshot_timestamp.Value;

            if (date_from_db != date_from_webpage)
            {
                this.snapshot_timestamp.Value = date_from_db;
                Bug.auto_subscribe(this.id);
                format_subcribe_cancel_link();
                bug_fields_have_changed = record_changes();
            }
            else
            {
                set_msg(Util.capitalize_first_letter(Util.get_setting("SingularBugLabel", "bug"))
                        + " was NOT updated.<br>"
                        + " Somebody changed it while you were editing it.<br>"
                        + " Click <a href=edit_bug.aspx?id="
                        + Convert.ToString(this.id)
                        + ">[here]</a> to refresh the page and discard your changes.<br>");
                return;
            }

            bugpost_fields_have_changed = Bug.insert_comment(this.id, this.security.user.usid, this.comment_formated,
                                              this.comment_search,
                                              null, // from
                                              null, // cc
                                              this.commentType, this.internal_only.Checked) != 0;

            if (bug_fields_have_changed || bugpost_fields_have_changed && !this.internal_only.Checked)
                Bug.send_notifications(Bug.UPDATE, this.id, this.security, 0, this.status_changed,
                    this.assigned_to_changed,
                    Convert.ToInt32(this.assigned_to.SelectedItem.Value));

            set_msg(Util.capitalize_first_letter(Util.get_setting("SingularBugLabel", "bug")) + " was updated.");

            this.comment.Value = "";

            set_controls_field_permission(this.permission_level);

            if (bug_fields_have_changed)
            {
                // Fetch again from database
                var updated_bug = Bug.get_bug_datarow(this.id, this.security, this.ds_custom_cols);

                // Allow for customization not written by me
                Workflow.custom_adjust_controls(updated_bug, this.security.user, this);
            }

            load_user_dropdown();
        }

        public void load_other_dropdowns_and_select_defaults(DataSet ds_defaults)
        {
            // org
            string default_value;

            default_value = Convert.ToString(this.security.user.org);
            foreach (ListItem li in this.org.Items)
                if (li.Value == default_value)
                    li.Selected = true;
                else
                    li.Selected = false;

            // category
            if (ds_defaults.Tables[1].Rows.Count > 0)
                default_value = Convert.ToString((int) ds_defaults.Tables[1].Rows[0][0]);
            else
                default_value = "0";

            foreach (ListItem li in this.category.Items)
                if (li.Value == default_value)
                    li.Selected = true;
                else
                    li.Selected = false;

            // priority
            if (ds_defaults.Tables[2].Rows.Count > 0)
                default_value = Convert.ToString((int) ds_defaults.Tables[2].Rows[0][0]);
            else
                default_value = "0";
            foreach (ListItem li in this.priority.Items)
                if (li.Value == default_value)
                    li.Selected = true;
                else
                    li.Selected = false;

            // status
            if (ds_defaults.Tables[3].Rows.Count > 0)
                default_value = Convert.ToString((int) ds_defaults.Tables[3].Rows[0][0]);
            else
                default_value = "0";
            foreach (ListItem li in this.status.Items)
                if (li.Value == default_value)
                    li.Selected = true;
                else
                    li.Selected = false;

            // udf
            if (ds_defaults.Tables[4].Rows.Count > 0)
                default_value = Convert.ToString((int) ds_defaults.Tables[4].Rows[0][0]);
            else
                default_value = "0";
            foreach (ListItem li in this.udf.Items)
                if (li.Value == default_value)
                    li.Selected = true;
                else
                    li.Selected = false;
        }

        public void load_project_and_user_dropdown_for_insert(DataTable project_default)
        {
            // get default values
            var initial_project = (string) Session["project"];

            // project
            if (this.security.user.forced_project != 0)
                initial_project = Convert.ToString(this.security.user.forced_project);

            if (initial_project != null && initial_project != "0")
            {
                foreach (ListItem li in this.project.Items)
                    if (li.Value == initial_project)
                        li.Selected = true;
                    else
                        li.Selected = false;
            }
            else
            {
                string default_value;
                if (project_default.Rows.Count > 0)
                    default_value = Convert.ToString((int) project_default.Rows[0][0]);
                else
                    default_value = "0";

                foreach (ListItem li in this.project.Items)
                    if (li.Value == default_value)
                        li.Selected = true;
                    else
                        li.Selected = false;
            }

            load_user_dropdown();
        }

        public void load_project_and_user_dropdown_for_update()
        {
            // Project
            if (this.prev_project.Value != "0")
            {
                // see if already in the dropdown.
                var already_in_dropdown = false;
                foreach (ListItem li in this.project.Items)
                    if (li.Value == this.prev_project.Value)
                    {
                        already_in_dropdown = true;
                        break;
                    }

                // Add to the list, even if permissions don't allow it now, because, in the past, they did allow it.
                if (!already_in_dropdown)
                    this.project.Items.Add(
                        new ListItem(this.prev_project_name.Value, this.prev_project.Value));
            }

            foreach (ListItem li in this.project.Items)
                if (li.Value == this.prev_project.Value)
                    li.Selected = true;
                else
                    li.Selected = false;

            load_user_dropdown();
        }

        public void load_user_dropdown()
        {
            // What's selected now?   Save it before we refresh the dropdown.
            var current_value = "";

            if (IsPostBack) current_value = this.assigned_to.SelectedItem.Value;

            // Load the user dropdown, which changes per project
            // Only users explicitly allowed will be listed
            if (Util.get_setting("DefaultPermissionLevel", "2") == "0")
                this.sql = @"
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
                this.sql = @"
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

            if (Util.get_setting("UseFullNames", "0") == "0")
                // false condition
                this.sql = this.sql.Replace("$fullnames", "0 = 1");
            else
                // true condition
                this.sql = this.sql.Replace("$fullnames", "1 = 1");

            if (this.project.SelectedItem != null)
                this.sql = this.sql.Replace("$pj", this.project.SelectedItem.Value);
            else
                this.sql = this.sql.Replace("$pj", "0");

            this.sql = this.sql.Replace("$og_id", Convert.ToString(this.security.user.org));
            this.sql = this.sql.Replace("$og_other_orgs_permission_level",
                Convert.ToString(this.security.user.other_orgs_permission_level));

            if (this.security.user.can_assign_to_internal_users)
                this.sql = this.sql.Replace("$og_can_assign_to_internal_users", "1 = 1");
            else
                this.sql = this.sql.Replace("$og_can_assign_to_internal_users", "0 = 1");

            this.dt_users = DbUtil.get_dataset(this.sql).Tables[0];

            this.assigned_to.DataSource = new DataView(this.dt_users);
            this.assigned_to.DataTextField = "us_username";
            this.assigned_to.DataValueField = "us_id";
            this.assigned_to.DataBind();
            this.assigned_to.Items.Insert(0, new ListItem("[not assigned]", "0"));

            // It can happen that the user in the db is not listed in the dropdown, because of a subsequent change in permissions.
            // Since that user IS the user associated with the bug, let's force it into the dropdown.
            if (this.id != 0) // if existing bug
                if (this.prev_assigned_to.Value != "0")
                {
                    // see if already in the dropdown.
                    var user_in_dropdown = false;
                    foreach (ListItem li in this.assigned_to.Items)
                        if (li.Value == this.prev_assigned_to.Value)
                        {
                            user_in_dropdown = true;
                            break;
                        }

                    // Add to the list, even if permissions don't allow it now, because, in the past, they did allow it.
                    if (!user_in_dropdown)
                        this.assigned_to.Items.Insert(1,
                            new ListItem(this.prev_assigned_to_username.Value, this.prev_assigned_to.Value));
                }

            // At this point, all the users we need are in the dropdown.
            // Now selected the selected.
            if (current_value == "") current_value = this.prev_assigned_to.Value;

            // Select the user.  We are either restoring the previous selection
            // or selecting what was in the database.
            if (current_value != "0")
                foreach (ListItem li in this.assigned_to.Items)
                    if (li.Value == current_value)
                        li.Selected = true;
                    else
                        li.Selected = false;

            // if nothing else is selected. select the default user for the project
            if (this.assigned_to.SelectedItem.Value == "0")
            {
                var project_default_user = 0;
                if (this.project.SelectedItem != null)
                {
                    // get the default user of the project
                    project_default_user = Util.get_default_user(Convert.ToInt32(this.project.SelectedItem.Value));

                    if (project_default_user != 0)
                        foreach (ListItem li in this.assigned_to.Items)
                            if (Convert.ToInt32(li.Value) == project_default_user)
                                li.Selected = true;
                            else
                                li.Selected = false;
                }
            }
        }

        public string get_custom_col_default_value(object o)
        {
            var defaultval = Convert.ToString(o);

            // populate the sql default value of a custom field
            if (defaultval.Length > 2)
                if (defaultval[0] == '('
                    && defaultval[defaultval.Length - 1] == ')')
                {
                    var defaultval_sql = "select " + defaultval.Substring(1, defaultval.Length - 2);
                    defaultval = Convert.ToString(DbUtil.execute_scalar(defaultval_sql));
                }

            return defaultval;
        }

        public void format_subcribe_cancel_link()
        {
            var notification_email_enabled = Util.get_setting("NotificationEmailEnabled", "1") == "1";
            if (notification_email_enabled)
            {
                int subscribed;
                if (!IsPostBack)
                {
                    subscribed = (int) this.dr_bug["subscribed"];
                }
                else
                {
                    // User might have changed bug to a project where we automatically subscribe
                    // so be prepared to format the link even if this isn't the first time in.
                    this.sql = "select count(1) from bug_subscriptions where bs_bug = $bg and bs_user = $us";
                    this.sql = this.sql.Replace("$bg", Convert.ToString(this.id));
                    this.sql = this.sql.Replace("$us", Convert.ToString(this.security.user.usid));
                    subscribed = (int) DbUtil.execute_scalar(this.sql);
                }

                if (this.security.user.is_guest) // wouldn't make sense to share an email address
                {
                    this.subscriptions.InnerHtml = "";
                }
                else
                {
                    var subscription_link =
                        "<a id='notifications' title='Get or stop getting email notifications about changes to this item.'"
                        + " href='javascript:toggle_notifications("
                        + Convert.ToString(this.id)
                        + ")'><img src=telephone.png border=0 align=top>&nbsp;<span id='get_stop_notifications'>";

                    if (subscribed > 0)
                        subscription_link += "stop notifications</span></a>";
                    else
                        subscription_link += "get notifications</span></a>";

                    this.subscriptions.InnerHtml = subscription_link;
                }
            }
        }

        public void set_org_field_permission(int bug_permission_level)
        {
            // pick the most restrictive permission
            var perm_level = bug_permission_level < this.security.user.org_field_permission_level
                ? bug_permission_level
                : this.security.user.org_field_permission_level;

            if (perm_level == Security.PERMISSION_NONE)
            {
                this.org_label.Visible = false;
                this.org.Visible = false;
                this.prev_org.Visible = false;
            }
            else if (perm_level == Security.PERMISSION_READONLY)
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
            if (this.id != 0)
            {
                this.static_short_desc.Style["display"] = "block";
                this.short_desc.Visible = false;
            }

            this.static_short_desc.InnerText = this.short_desc.Value;
        }

        public void set_tags_field_permission(int bug_permission_level)
        {
            /// JUNK testing using cat permission
            // pick the most restrictive permission
            var perm_level = bug_permission_level < this.security.user.tags_field_permission_level
                ? bug_permission_level
                : this.security.user.tags_field_permission_level;

            if (perm_level == Security.PERMISSION_NONE)
            {
                this.static_tags.Visible = false;
                this.tags_label.Visible = false;
                this.tags.Visible = false;
                this.tags_link.Visible = false;
                this.prev_tags.Visible = false;
                //tags_row.Style.display = "none";
            }
            else if (perm_level == Security.PERMISSION_READONLY)
            {
                if (this.id != 0)
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

        public void set_category_field_permission(int bug_permission_level)
        {
            // pick the most restrictive permission
            var perm_level = bug_permission_level < this.security.user.category_field_permission_level
                ? bug_permission_level
                : this.security.user.category_field_permission_level;

            if (perm_level == Security.PERMISSION_NONE)
            {
                this.category_label.Visible = false;
                this.category.Visible = false;
                this.prev_category.Visible = false;
            }
            else if (perm_level == Security.PERMISSION_READONLY)
            {
                this.category.Visible = false;
                this.static_category.InnerText = this.category.SelectedItem.Text;
            }
            else // editable
            {
                this.static_category.Visible = false;
            }
        }

        public void set_priority_field_permission(int bug_permission_level)
        {
            // pick the most restrictive permission
            var perm_level = bug_permission_level < this.security.user.priority_field_permission_level
                ? bug_permission_level
                : this.security.user.priority_field_permission_level;

            if (perm_level == Security.PERMISSION_NONE)
            {
                this.priority_label.Visible = false;
                this.priority.Visible = false;
                this.prev_priority.Visible = false;
            }
            else if (perm_level == Security.PERMISSION_READONLY)
            {
                this.priority.Visible = false;
                this.static_priority.InnerText = this.priority.SelectedItem.Text;
            }
            else // editable
            {
                this.static_priority.Visible = false;
            }
        }

        public void set_status_field_permission(int bug_permission_level)
        {
            // pick the most restrictive permission
            var perm_level = bug_permission_level < this.security.user.status_field_permission_level
                ? bug_permission_level
                : this.security.user.status_field_permission_level;

            if (perm_level == Security.PERMISSION_NONE)
            {
                this.status_label.Visible = false;
                this.status.Visible = false;
                this.prev_status.Visible = false;
            }
            else if (perm_level == Security.PERMISSION_READONLY)
            {
                this.status.Visible = false;
                this.static_status.InnerText = this.status.SelectedItem.Text;
            }
            else // editable
            {
                this.static_status.Visible = false;
            }
        }

        public void set_project_field_permission(int bug_permission_level)
        {
            var perm_level = bug_permission_level < this.security.user.project_field_permission_level
                ? bug_permission_level
                : this.security.user.project_field_permission_level;

            if (perm_level == Security.PERMISSION_NONE)
            {
                this.project_label.Visible = false;
                this.project.Visible = false;
                this.prev_project.Visible = false;
            }
            else if (perm_level == Security.PERMISSION_READONLY)
            {
                this.project.Visible = false;
                this.static_project.InnerText = this.project.SelectedItem.Text;
            }
            else
            {
                this.static_project.Visible = false;
            }
        }

        public void set_assigned_field_permission(int bug_permission_level)
        {
            var perm_level = bug_permission_level < this.security.user.assigned_to_field_permission_level
                ? bug_permission_level
                : this.security.user.assigned_to_field_permission_level;

            if (perm_level == Security.PERMISSION_NONE)
            {
                this.assigned_to_label.Visible = false;
                this.assigned_to.Visible = false;
                this.prev_assigned_to.Visible = false;
            }
            else if (perm_level == Security.PERMISSION_READONLY)
            {
                this.assigned_to.Visible = false;
                this.static_assigned_to.InnerText = this.assigned_to.SelectedItem.Text;
            }
        }

        public void set_udf_field_permission(int bug_permission_level)
        {
            // pick the most restrictive permission
            var perm_level = bug_permission_level < this.security.user.udf_field_permission_level
                ? bug_permission_level
                : this.security.user.udf_field_permission_level;

            if (perm_level == Security.PERMISSION_NONE)
            {
                this.udf_label.Visible = false;
                this.udf.Visible = false;
                this.prev_udf.Visible = false;
            }
            else if (perm_level == Security.PERMISSION_READONLY)
            {
                this.udf.Visible = false;
                this.static_udf.InnerText = this.udf.SelectedItem.Text;
            }
            else // editable
            {
                this.static_udf.Visible = false;
            }
        }

        public void set_controls_field_permission(int bug_permission_level)
        {
            if (bug_permission_level == Security.PERMISSION_READONLY
                || bug_permission_level == Security.PERMISSION_REPORTER)
            {
                // even turn off commenting updating for read only
                if (this.permission_level == Security.PERMISSION_READONLY)
                {
                    this.submit_button.Disabled = true;
                    this.submit_button.Visible = false;
                    if (Util.get_setting("DisplayAnotherButtonInEditBugPage", "0") == "1")
                    {
                        this.submit_button2.Disabled = true;
                        this.submit_button2.Visible = false;
                    }

                    this.comment_label.Visible = false;
                    this.comment.Visible = false;
                }

                set_project_field_permission(Security.PERMISSION_READONLY);
                set_org_field_permission(Security.PERMISSION_READONLY);
                set_category_field_permission(Security.PERMISSION_READONLY);
                set_tags_field_permission(Security.PERMISSION_READONLY);
                set_priority_field_permission(Security.PERMISSION_READONLY);
                set_status_field_permission(Security.PERMISSION_READONLY);
                set_assigned_field_permission(Security.PERMISSION_READONLY);
                set_udf_field_permission(Security.PERMISSION_READONLY);
                set_shortdesc_field_permission();

                this.internal_only_label.Visible = false;
                this.internal_only.Visible = false;
            }
            else
            {
                // Call these functions so that the field level permissions can kick in
                if (this.security.user.forced_project != 0)
                    set_project_field_permission(Security.PERMISSION_READONLY);
                else
                    set_project_field_permission(Security.PERMISSION_ALL);

                if (this.security.user.other_orgs_permission_level == 0)
                    set_org_field_permission(Security.PERMISSION_READONLY);
                else
                    set_org_field_permission(Security.PERMISSION_ALL);
                set_category_field_permission(Security.PERMISSION_ALL);
                set_tags_field_permission(Security.PERMISSION_ALL);
                set_priority_field_permission(Security.PERMISSION_ALL);
                set_status_field_permission(Security.PERMISSION_ALL);
                set_assigned_field_permission(Security.PERMISSION_ALL);
                set_udf_field_permission(Security.PERMISSION_ALL);
            }
        }

        public void format_prev_next_bug()
        {
            // for next/prev bug links
            var dv_bugs = (DataView) Session["bugs"];

            if (dv_bugs != null)
            {
                var prev_bug = 0;
                var next_bug = 0;
                var this_bug_found = false;

                // read through the list of bugs looking for the one that matches this one
                var position_in_list = 0;
                var save_position_in_list = 0;
                foreach (DataRowView drv in dv_bugs)
                {
                    position_in_list++;
                    if (this_bug_found)
                    {
                        // step 3 - get the next bug - we're done
                        next_bug = (int) drv[1];
                        break;
                    }

                    if (this.id == (int) drv[1])
                    {
                        // step 2 - we found this - set switch
                        save_position_in_list = position_in_list;
                        this_bug_found = true;
                    }
                    else
                    {
                        // step 1 - save the previous just in case the next one IS this bug
                        prev_bug = (int) drv[1];
                    }
                }

                var prev_next_link = "";

                if (this_bug_found)
                {
                    if (prev_bug != 0)
                        prev_next_link =
                            "&nbsp;&nbsp;&nbsp;&nbsp;<a class=warn href=edit_bug.aspx?id="
                            + Convert.ToString(prev_bug)
                            + "><img src=arrow_up.png border=0 align=top>prev</a>";
                    else
                        prev_next_link = "&nbsp;&nbsp;&nbsp;&nbsp;<span class=gray_link>prev</span>";

                    if (next_bug != 0)
                        prev_next_link +=
                            "&nbsp;&nbsp;&nbsp;&nbsp;<a class=warn href=edit_bug.aspx?id="
                            + Convert.ToString(next_bug)
                            + ">next<img src=arrow_down.png border=0 align=top></a>";
                    else
                        prev_next_link += "&nbsp;&nbsp;&nbsp;&nbsp;<span class=gray_link>next</span>";

                    prev_next_link += "&nbsp;&nbsp;&nbsp;<span class=smallnote>"
                                      + Convert.ToString(save_position_in_list)
                                      + " of "
                                      + Convert.ToString(dv_bugs.Count)
                                      + "</span>";

                    this.prev_next.InnerHtml = prev_next_link;
                }
            }
        }

        public void load_dropdowns(User user)
        {
            // only show projects where user has permissions
            // 0
            this.sql = @"/* drop downs */ select pj_id, pj_name
		from projects
		left outer join project_user_xref on pj_id = pu_project
		and pu_user = $us
		where pj_active = 1
		and isnull(pu_permission_level,$dpl) not in (0, 1)
		order by pj_name;";

            this.sql = this.sql.Replace("$us", Convert.ToString(this.security.user.usid));
            this.sql = this.sql.Replace("$dpl", Util.get_setting("DefaultPermissionLevel", "2"));

            // 1
            this.sql += "\nselect og_id, og_name from orgs where og_active = 1 order by og_name;";

            // 2
            this.sql += "\nselect ct_id, ct_name from categories order by ct_sort_seq, ct_name;";

            // 3
            this.sql += "\nselect pr_id, pr_name from priorities order by pr_sort_seq, pr_name;";

            // 4
            this.sql += "\nselect st_id, st_name from statuses order by st_sort_seq, st_name;";

            // 5
            this.sql += "\nselect udf_id, udf_name from user_defined_attribute order by udf_sort_seq, udf_name;";

            // do a batch of sql statements
            var ds_dropdowns = DbUtil.get_dataset(this.sql);

            this.project.DataSource = ds_dropdowns.Tables[0];
            this.project.DataTextField = "pj_name";
            this.project.DataValueField = "pj_id";
            this.project.DataBind();

            if (Util.get_setting("DefaultPermissionLevel", "2") == "2")
                this.project.Items.Insert(0, new ListItem("[no project]", "0"));

            this.org.DataSource = ds_dropdowns.Tables[1];
            this.org.DataTextField = "og_name";
            this.org.DataValueField = "og_id";
            this.org.DataBind();
            this.org.Items.Insert(0, new ListItem("[no organization]", "0"));

            this.category.DataSource = ds_dropdowns.Tables[2];
            this.category.DataTextField = "ct_name";
            this.category.DataValueField = "ct_id";
            this.category.DataBind();
            this.category.Items.Insert(0, new ListItem("[no category]", "0"));

            this.priority.DataSource = ds_dropdowns.Tables[3];
            this.priority.DataTextField = "pr_name";
            this.priority.DataValueField = "pr_id";
            this.priority.DataBind();
            this.priority.Items.Insert(0, new ListItem("[no priority]", "0"));

            this.status.DataSource = ds_dropdowns.Tables[4];
            this.status.DataTextField = "st_name";
            this.status.DataValueField = "st_id";
            this.status.DataBind();
            this.status.Items.Insert(0, new ListItem("[no status]", "0"));

            this.udf.DataSource = ds_dropdowns.Tables[5];
            this.udf.DataTextField = "udf_name";
            this.udf.DataValueField = "udf_id";
            this.udf.DataBind();
            this.udf.Items.Insert(0, new ListItem("[none]", "0"));
        }

        public string get_dropdown_text_from_value(DropDownList dropdown, string value)
        {
            foreach (ListItem li in dropdown.Items)
                if (li.Value == value)
                    return li.Text;

            return dropdown.Items[0].Text;
        }

        public bool did_something_change()
        {
            var something_changed = false;

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
                || Util.get_setting("ShowUserDefinedBugAttribute", "1") == "1" &&
                this.prev_udf.Value != this.udf.SelectedItem.Value)
            {
                this.clone_ignore_bugid.Value = "0";
                something_changed = true;
            }

            // Now look to see if custom fields changed
            if (!something_changed)
                foreach (var column_name in this.hash_custom_cols.Keys)
                {
                    var after = this.hash_custom_cols[column_name];
                    if (after == null) continue; // because there's no control, nothing for user to edit

                    var before = Util.format_db_value(this.dr_bug[column_name]);

                    if (before != after.Trim())
                    {
                        something_changed = true;
                        break;
                    }
                }

            if (!something_changed)
                if (Request["pcd1"] != null && this.prev_pcd1.Value != Request["pcd1"]
                    || Request["pcd2"] != null && this.prev_pcd2.Value != Request["pcd2"]
                    || Request["pcd3"] != null && this.prev_pcd3.Value != Request["pcd3"])
                    something_changed = true;

            return something_changed;
        }

        // returns true if there was a change
        public bool record_changes()
        {
            var base_sql = @"
		insert into bug_posts
		(bp_bug, bp_user, bp_date, bp_comment, bp_type)
		values($id, $us, getdate(), N'$update_msg', 'update')";

            base_sql = base_sql.Replace("$id", Convert.ToString(this.id));
            base_sql = base_sql.Replace("$us", Convert.ToString(this.security.user.usid));

            string from;
            this.sql = "";

            var do_update = false;

            if (this.prev_short_desc.Value != this.short_desc.Value)
            {
                do_update = true;
                this.sql += base_sql.Replace(
                    "$update_msg",
                    "changed desc from \""
                    + this.prev_short_desc.Value.Replace("'", "''") + "\" to \""
                    + this.short_desc.Value.Replace("'", "''") + "\"");

                this.prev_short_desc.Value = this.short_desc.Value;
            }

            if (this.prev_tags.Value != this.tags.Value)
            {
                do_update = true;
                this.sql += base_sql.Replace(
                    "$update_msg",
                    "changed tags from \""
                    + this.prev_tags.Value.Replace("'", "''") + "\" to \""
                    + this.tags.Value.Replace("'", "''") + "\"");

                this.prev_tags.Value = this.tags.Value;

                if (Util.get_setting("EnableTags", "0") == "1") Tags.build_tag_index(Application);
            }

            if (this.project.SelectedItem.Value != this.prev_project.Value)
            {
                // The "from" might not be in the dropdown anymore
                //from = get_dropdown_text_from_value(project, prev_project.Value);

                do_update = true;
                this.sql += base_sql.Replace(
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

                do_update = true;
                this.sql += base_sql.Replace(
                    "$update_msg",
                    "changed organization from \""
                    + from.Replace("'", "''") + "\" to \""
                    + this.org.SelectedItem.Text.Replace("'", "''") + "\"");

                this.prev_org.Value = this.org.SelectedItem.Value;
            }

            if (this.prev_category.Value != this.category.SelectedItem.Value)
            {
                from = get_dropdown_text_from_value(this.category, this.prev_category.Value);

                do_update = true;
                this.sql += base_sql.Replace(
                    "$update_msg",
                    "changed category from \""
                    + from.Replace("'", "''") + "\" to \""
                    + this.category.SelectedItem.Text.Replace("'", "''") + "\"");

                this.prev_category.Value = this.category.SelectedItem.Value;
            }

            if (this.prev_priority.Value != this.priority.SelectedItem.Value)
            {
                from = get_dropdown_text_from_value(this.priority, this.prev_priority.Value);

                do_update = true;
                this.sql += base_sql.Replace(
                    "$update_msg",
                    "changed priority from \""
                    + from.Replace("'", "''") + "\" to \""
                    + this.priority.SelectedItem.Text.Replace("'", "''") + "\"");

                this.prev_priority.Value = this.priority.SelectedItem.Value;
            }

            if (this.prev_assigned_to.Value != this.assigned_to.SelectedItem.Value)
            {
                this.assigned_to_changed = true; // for notifications

                // The "from" might not be in the dropdown anymore...
                //from = get_dropdown_text_from_value(assigned_to, prev_assigned_to.Value);

                do_update = true;
                this.sql += base_sql.Replace(
                    "$update_msg",
                    "changed assigned_to from \""
                    + this.prev_assigned_to_username.Value.Replace("'", "''") + "\" to \""
                    + this.assigned_to.SelectedItem.Text.Replace("'", "''") + "\"");

                this.prev_assigned_to.Value = this.assigned_to.SelectedItem.Value;
                this.prev_assigned_to_username.Value = this.assigned_to.SelectedItem.Text;
            }

            if (this.prev_status.Value != this.status.SelectedItem.Value)
            {
                this.status_changed = true; // for notifications

                from = get_dropdown_text_from_value(this.status, this.prev_status.Value);

                do_update = true;
                this.sql += base_sql.Replace(
                    "$update_msg",
                    "changed status from \""
                    + from.Replace("'", "''") + "\" to \""
                    + this.status.SelectedItem.Text.Replace("'", "''") + "\"");

                this.prev_status.Value = this.status.SelectedItem.Value;
            }

            if (Util.get_setting("ShowUserDefinedBugAttribute", "1") == "1")
                if (this.prev_udf.Value != this.udf.SelectedItem.Value)
                {
                    from = get_dropdown_text_from_value(this.udf, this.prev_udf.Value);

                    do_update = true;
                    this.sql += base_sql.Replace(
                        "$update_msg",
                        "changed " + Util.get_setting("UserDefinedBugAttributeName", "YOUR ATTRIBUTE")
                                   + " from \""
                                   + from.Replace("'", "''") + "\" to \""
                                   + this.udf.SelectedItem.Text.Replace("'", "''") + "\"");

                    this.prev_udf.Value = this.udf.SelectedItem.Value;
                }

            // Record changes in custom columns

            foreach (DataRow drcc in this.ds_custom_cols.Tables[0].Rows)
            {
                var column_name = (string) drcc["name"];

                if (this.security.user.dict_custom_field_permission_level[column_name] !=
                    Security.PERMISSION_ALL) continue;

                var before = Util.format_db_value(this.dr_bug[column_name]);
                var after = this.hash_custom_cols[column_name];

                if (before == "0") before = "";

                if (after == "0") after = "";

                if (before.Trim() != after.Trim())
                {
                    if ((string) drcc["dropdown type"] == "users")
                    {
                        var sql_get_username = "";
                        if (before == "")
                        {
                            before = "";
                        }
                        else
                        {
                            sql_get_username = "select us_username from users where us_id = $1";
                            before = (string) DbUtil.execute_scalar(sql_get_username.Replace("$1",
                                Util.sanitize_integer(before)));
                        }

                        if (after == "")
                        {
                            after = "";
                        }
                        else
                        {
                            sql_get_username = "select us_username from users where us_id = $1";
                            after = (string) DbUtil.execute_scalar(sql_get_username.Replace("$1",
                                Util.sanitize_integer(after)));
                        }
                    }

                    do_update = true;
                    this.sql += base_sql.Replace(
                        "$update_msg",
                        "changed " + column_name + " from \"" + before.Trim().Replace("'", "''") + "\" to \"" +
                        after.Trim().Replace("'", "''") + "\"");
                }
            }

            // Handle project custom dropdowns
            if (Request["label_pcd1"] != null && Request["pcd1"] != null && this.prev_pcd1.Value != Request["pcd1"])
            {
                do_update = true;
                this.sql += base_sql.Replace(
                    "$update_msg",
                    "changed "
                    + Request["label_pcd1"].Replace("'", "''")
                    + " from \"" + this.prev_pcd1.Value + "\" to \"" + Request["pcd1"].Replace("'", "''") + "\"");

                this.prev_pcd1.Value = Request["pcd1"];
            }

            if (Request["label_pcd2"] != null && Request["pcd2"] != null &&
                this.prev_pcd2.Value != Request["pcd2"].Replace("'", "''"))
            {
                do_update = true;
                this.sql += base_sql.Replace(
                    "$update_msg",
                    "changed "
                    + Request["label_pcd2"].Replace("'", "''")
                    + " from \"" + this.prev_pcd2.Value + "\" to \"" + Request["pcd2"].Replace("'", "''") + "\"");

                this.prev_pcd2.Value = Request["pcd2"];
            }

            if (Request["label_pcd3"] != null && Request["pcd3"] != null && this.prev_pcd3.Value != Request["pcd3"])
            {
                do_update = true;
                this.sql += base_sql.Replace(
                    "$update_msg",
                    "changed "
                    + Request["label_pcd3"].Replace("'", "''")
                    + " from \"" + this.prev_pcd3.Value + "\" to \"" + Request["pcd3"].Replace("'", "''") + "\"");

                this.prev_pcd3.Value = Request["pcd3"];
            }

            if (do_update
                && Util.get_setting("TrackBugHistory", "1") == "1") // you might not want the debris to grow
                DbUtil.execute_nonquery(this.sql);

            if (this.project.SelectedItem.Value != this.prev_project.Value)
                this.permission_level = fetch_permission_level(this.project.SelectedItem.Value);

            // return true if something did change
            return do_update;
        }

        public int fetch_permission_level(string projectToCheck)
        {
            // fetch the revised permission level
            this.sql = @"declare @permission_level int
		set @permission_level = -1
		select @permission_level = isnull(pu_permission_level,$dpl)
		from project_user_xref
		where pu_project = $pj
		and pu_user = $us
		if @permission_level = -1 set @permission_level = $dpl
		select @permission_level";

            this.sql = this.sql.Replace("$dpl", Util.get_setting("DefaultPermissionLevel", "2"));
            this.sql = this.sql.Replace("$pj", projectToCheck);
            this.sql = this.sql.Replace("$us", Convert.ToString(this.security.user.usid));
            var pl = (int) DbUtil.execute_scalar(this.sql);

            // reduce permissions for guest
            //if (security.user.is_guest && permission_level == Security.PERMISSION_ALL)
            //{
            //	pl = Security.PERMISSION_REPORTER;
            //}

            return pl;
        }

        public bool validate()
        {
            var good = true;
            this.custom_validation_err_msg.InnerText = "";

            if (this.short_desc.Value == "")
            {
                good = false;
                this.short_desc_err.InnerText = "Short Description is required.";
            }
            else
            {
                this.short_desc_err.InnerText = "";
            }

            if (!did_something_change()) return false;

            // validate custom columns
            foreach (DataRow drcc in this.ds_custom_cols.Tables[0].Rows)
            {
                var name = (string) drcc["name"];

                if (this.security.user.dict_custom_field_permission_level[name] != Security.PERMISSION_ALL) continue;

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
                            DateTime.Parse(val, Util.get_culture_info());
                        }
                        catch (FormatException)
                        {
                            append_custom_field_msg("\"" + name + "\" not in a valid date format.<br>");
                            good = false;
                        }
                    }
                    else if (datatype == "int")
                    {
                        if (!Util.is_int(val))
                        {
                            append_custom_field_msg("\"" + name + "\" must be an integer.<br>");
                            good = false;
                        }
                    }
                    else if (datatype == "decimal")
                    {
                        var xprec = Convert.ToInt32(drcc["xprec"]);
                        var xscale = Convert.ToInt32(drcc["xscale"]);

                        var decimal_error = Util.is_valid_decimal(name, val, xprec - xscale, xscale);
                        if (decimal_error != "")
                        {
                            append_custom_field_msg(decimal_error + "<br>");
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
                this.assigned_to_err.InnerText = "";
            }

            // custom validations go here
            if (!Workflow.custom_validations(this.dr_bug, this.security.user,
                this, this.custom_validation_err_msg))
                good = false;

            return good;
        }

        public bool does_assigned_to_have_permission_for_org(int assigned_to, int org)
        {
            if (assigned_to < 1) return true;

            var sql = @"
/* validate org versus assigned_to */
select case when og_other_orgs_permission_level <> 0
or $bg_org = og_id then 1
else 0 end as [answer]
from users
inner join orgs on us_org = og_id
where us_id = @us_id";

            sql = sql.Replace("@us_id", Convert.ToString(assigned_to));
            sql = sql.Replace("$bg_org", Convert.ToString(org));

            var allowed = DbUtil.execute_scalar(sql);

            if (allowed != null && Convert.ToInt32(allowed) == 1)
                return true;
            return false;
        }

        public void set_msg(string s)
        {
            this.msg.InnerHtml = s;
            if (Util.get_setting("DisplayAnotherButtonInEditBugPage", "0") == "1") this.msg2.InnerHtml = s;
        }

        private void set_custom_field_msg(string s)
        {
            this.custom_field_msg.InnerHtml = s;
            if (Util.get_setting("DisplayAnotherButtonInEditBugPage", "0") == "1") this.custom_field_msg2.InnerHtml = s;
        }

        public void append_custom_field_msg(string s)
        {
            this.custom_field_msg.InnerHtml += s;
            if (Util.get_setting("DisplayAnotherButtonInEditBugPage", "0") == "1")
                this.custom_field_msg2.InnerHtml += s;
        }

        public void display_custom_fields()
        {
            var minTextAreaSize = int.Parse(Util.get_setting("TextAreaThreshold", "100"));
            var maxTextAreaRows = int.Parse(Util.get_setting("MaxTextAreaRows", "5"));

            // Create the custom column INPUT elements
            foreach (DataRow drcc in this.ds_custom_cols.Tables[0].Rows)
            {
                var column_name = (string) drcc["name"];

                var field_permission_level = this.security.user.dict_custom_field_permission_level[column_name];
                if (field_permission_level == Security.PERMISSION_NONE) continue;

                var field_id = column_name.Replace(" ", "");

                Response.Write("\n<tr id=\"" + field_id + "_row\">");
                Response.Write("<td nowrap><span id=\"" + field_id + "_label\">");
                Response.Write(column_name);

                var permission_on_original = this.permission_level;

                if (this.prev_project.Value != string.Empty
                    && (this.project.SelectedItem == null ||
                        this.project.SelectedItem.Value != this.prev_project.Value))
                    permission_on_original = fetch_permission_level(this.prev_project.Value);

                if (permission_on_original == Security.PERMISSION_READONLY
                    || permission_on_original == Security.PERMISSION_REPORTER)
                    Response.Write(":</span><td align=left width=600px>");
                else
                    Response.Write(":</span><td align=left>");

                //20040413 WWR - If a custom database field is over the defined character length, use a TextArea control
                var fieldLength = int.Parse(drcc["length"].ToString());
                var datatype = drcc["datatype"].ToString();

                var dropdown_type = Convert.ToString(drcc["dropdown type"]);

                if (permission_on_original == Security.PERMISSION_READONLY
                    || field_permission_level == Security.PERMISSION_READONLY)
                {
                    string text;

                    if (this.id == 0) // add
                    {
                        text = get_custom_col_default_value(drcc["default value"]);
                    }
                    else
                    {
                        text = Convert.ToString(this.dr_bug[column_name]);

                        if (datatype == "datetime") text = Util.format_db_date_and_time(text);
                    }

                    if (fieldLength > minTextAreaSize && !string.IsNullOrEmpty(text))
                    {
                        // more readable if there is a lot of text
                        Response.Write("<div class='short_desc_static'  id=\"" + field_id + "_static\"><pre>");
                        Response.Write(HttpUtility.HtmlEncode(text));
                        Response.Write("</pre></div>");
                    }
                    else
                    {
                        Response.Write("<span class='stat' id=\"" + field_id + "_static\">");
                        if (dropdown_type == "users")
                        {
                            if (!string.IsNullOrEmpty(text))
                            {
                                var view_only_user_id = Convert.ToInt32(text);
                                var dv_users = new DataView(this.dt_users);
                                foreach (DataRowView drv in dv_users)
                                    if (view_only_user_id == (int) drv[0])
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
                        && dropdown_type != "normal"
                        && dropdown_type != "users")
                    {
                        Response.Write("<textarea class='txt resizable'");
                        Response.Write(" onkeydown=\"return count_chars('" + field_id + "'," + fieldLength + ")\" ");
                        Response.Write(" onkeyup=\"return count_chars('" + field_id + "'," + fieldLength + ")\" ");
                        Response.Write(" cols=\"" + minTextAreaSize + "\" rows=\"" +
                                       (fieldLength / minTextAreaSize > maxTextAreaRows
                                           ? maxTextAreaRows
                                           : fieldLength / minTextAreaSize) + "\" ");
                        Response.Write(" name=\"" + column_name + "\"");
                        Response.Write(" id=\"" + field_id + "\" >");
                        Response.Write(HttpUtility.HtmlEncode(this.hash_custom_cols[column_name]));
                        Response.Write("</textarea><div class=smallnote id=\"" + field_id + "_cnt\">&nbsp;</div>");
                    }
                    else
                    {
                        var dropdown_vals = Convert.ToString(drcc["vals"]);

                        if (dropdown_type != "" || dropdown_vals != "")
                        {
                            var selected_value = this.hash_custom_cols[column_name].Trim();

                            Response.Write("<select ");

                            Response.Write(" id=\"" + field_id + "\"");
                            Response.Write(" name=\"" + column_name + "\"");
                            Response.Write(">");

                            if (dropdown_type != "users")
                            {
                                var options = Util.split_dropdown_vals(dropdown_vals);
                                var decoded_selected_value = HttpUtility.HtmlDecode(selected_value);
                                for (var j = 0; j < options.Length; j++)
                                {
                                    Response.Write("<option");
                                    var decoded_option = HttpUtility.HtmlDecode(options[j]);
                                    if (decoded_option == decoded_selected_value) Response.Write(" selected ");
                                    Response.Write(">");
                                    Response.Write(decoded_option);
                                    Response.Write("</option>");
                                }
                            }
                            else
                            {
                                Response.Write("<option value=0>[not selected]</option>");

                                var dv_users = new DataView(this.dt_users);
                                foreach (DataRowView drv in dv_users)
                                {
                                    var user_id = Convert.ToString(drv[0]);
                                    var user_name = Convert.ToString(drv[1]);

                                    Response.Write("<option value=");
                                    Response.Write(user_id);

                                    if (user_id == selected_value) Response.Write(" selected ");
                                    Response.Write(">");
                                    Response.Write(user_name);
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

                            Response.Write(" name=\"" + column_name + "\"");
                            Response.Write(" id=\"" + field_id + "\"");
                            Response.Write(" value=\"");
                            Response.Write(this.hash_custom_cols[column_name].Replace("\"", "&quot;"));

                            if (datatype == "datetime")
                            {
                                Response.Write("\" class='txt date'  >");
                                Response.Write("<a style=\"font-size: 8pt;\"href=\"javascript:show_calendar('"
                                               + field_id
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

        public void display_project_specific_custom_fields()
        {
            // create project custom dropdowns
            if (this.project.SelectedItem != null
                && this.project.SelectedItem.Value != null
                && this.project.SelectedItem.Value != "0")
            {
                this.sql = @"select
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

                this.sql = this.sql.Replace("$pj", this.project.SelectedItem.Value);

                var project_dr = DbUtil.get_datarow(this.sql);

                if (project_dr != null)
                    for (var i = 1; i < 4; i++)
                        if ((int) project_dr["pj_enable_custom_dropdown" + Convert.ToString(i)] == 1)
                        {
                            // GC: 20-Feb-08: Modified to add an ID to each custom row for CSS customisation
                            Response.Write("\n<tr id=\"pcdrow" + Convert.ToString(i) + "\"><td nowrap>");

                            Response.Write("<span id=label_pcd" + Convert.ToString(i) + ">");
                            Response.Write(project_dr["pj_custom_dropdown_label" + Convert.ToString(i)]);
                            Response.Write("</span>");
                            // End GC
                            Response.Write("<td nowrap>");

                            var permission_on_original = this.permission_level;
                            if (this.prev_project.Value != string.Empty &&
                                this.project.SelectedItem.Value != this.prev_project.Value)
                                permission_on_original = fetch_permission_level(this.prev_project.Value);

                            if (permission_on_original == Security.PERMISSION_READONLY
                                || permission_on_original == Security.PERMISSION_REPORTER)
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
                                    if (this.id != 0)
                                    {
                                        var val = (string) this.dr_bug[
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
                                Response.Write(project_dr["pj_custom_dropdown_label" + Convert.ToString(i)]);
                                Response.Write("\">");

                                // create a dropdown

                                Response.Write("<select");
                                // GC: 20-Feb-08: Added an ID as well for easier CSS customisation
                                Response.Write(" name=pcd" + Convert.ToString(i));
                                Response.Write(" id=pcd" + Convert.ToString(i) + ">");
                                var options = Util.split_dropdown_vals(
                                    (string) project_dr["pj_custom_dropdown_values" + Convert.ToString(i)]);

                                var selected_value = "";

                                if (IsPostBack)
                                {
                                    selected_value = Request["pcd" + Convert.ToString(i)];
                                }
                                else
                                {
                                    // first time viewing existing
                                    if (this.id != 0)
                                        selected_value =
                                            (string) this.dr_bug[
                                                "bg_project_custom_dropdown_value" + Convert.ToString(i)];
                                }

                                for (var j = 0; j < options.Length; j++)
                                {
                                    Response.Write("<option value=\"" + options[j] + "\"");

                                    //if (options[j] == selected_value)
                                    if (HttpUtility.HtmlDecode(options[j]) == selected_value)
                                        Response.Write(" selected ");
                                    Response.Write(">");
                                    Response.Write(options[j]);
                                }

                                Response.Write("</select>");
                            }
                        }
            }
        }

        public void display_bug_relationships()
        {
            this.ds_posts = PrintBug.get_bug_posts(this.id, this.security.user.external_user, this.history_inline);
            var link_marker = Util.get_setting("BugLinkMarker", "bugid#");
            var reLinkMarker = new Regex(link_marker + "([0-9]+)");
            var dict_linked_bugs = new SortedDictionary<int, int>();

            // fish out bug links
            foreach (DataRow dr_post in this.ds_posts.Tables[0].Rows)
                if ((string) dr_post["bp_type"] == "comment")
                {
                    var match_collection = reLinkMarker.Matches((string) dr_post["bp_comment"]);

                    foreach (Match match in match_collection)
                    {
                        var other_bugid = Convert.ToInt32(match.Groups[1].ToString());
                        if (other_bugid != this.id) dict_linked_bugs[other_bugid] = 1;
                    }
                }

            if (dict_linked_bugs.Count > 0)
            {
                Response.Write("Linked to:");
                foreach (var int_other_bugid in dict_linked_bugs.Keys)
                {
                    var string_other_bugid = Convert.ToString(int_other_bugid);

                    Response.Write("&nbsp;<a href=edit_bug.aspx?id=");
                    Response.Write(string_other_bugid);
                    Response.Write(">");
                    Response.Write(string_other_bugid);
                    Response.Write("</a>");
                }
            }
        }
    }
}
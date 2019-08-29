/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Data;
    using System.Web;
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using Core;

    public partial class mbug : Page
    {
        public bool assigned_to_changed;
        public DataSet ds_posts;
        public string err_text;
        public int id;

        public int permission_level;
        public Security security;
        public string sql;
        public bool status_changed;

        //SortedDictionary<string, string> hash_custom_cols = new SortedDictionary<string, string>();
        //SortedDictionary<string, string> hash_prev_custom_cols = new SortedDictionary<string, string>();

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);
            if (Util.get_setting("EnableMobile", "0") == "0")
            {
                Response.Write("BugTracker.NET EnableMobile is not set to 1 in Web.config");
                Response.End();
            }

            this.msg.InnerText = "";
            this.err_text = "";

            var string_bugid = Request["id"];

            if (string_bugid == null || string_bugid == "" || string_bugid == "0")
            {
                this.id = 0;

                this.submit_button.Value = "Create";
                Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - Create ";
                this.my_header.InnerText = Page.Title;

                if (IsPostBack)
                {
                    if (!validate())
                    {
                        this.msg.InnerHtml = this.err_text;
                    }
                    else
                    {
                        var result = insert_bug();
                        if (result != "")
                            this.msg.InnerHtml = this.err_text;
                        else
                            Response.Redirect("mbugs.aspx");
                    }
                }
                else
                {
                    load_dropdowns(this.security.user);

                    this.sql = "\nselect top 1 pj_id from projects where pj_default = 1 order by pj_name;"; // 0
                    this.sql += "\nselect top 1 st_id from statuses where st_default = 1 order by st_name;"; // 1

                    var ds_defaults = DbUtil.get_dataset(this.sql);
                    var dt_project_default = ds_defaults.Tables[0];
                    var dt_status_default = ds_defaults.Tables[1];

                    string default_value;

                    // status
                    if (ds_defaults.Tables[1].Rows.Count > 0)
                        default_value = Convert.ToString((int) dt_status_default.Rows[0][0]);
                    else
                        default_value = "0";
                    foreach (ListItem li in this.status.Items)
                        if (li.Value == default_value)
                            li.Selected = true;
                        else
                            li.Selected = false;

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
                        if (dt_project_default.Rows.Count > 0)
                            default_value = Convert.ToString((int) dt_project_default.Rows[0][0]);
                        else
                            default_value = "0";

                        foreach (ListItem li in this.project.Items)
                            if (li.Value == default_value)
                                li.Selected = true;
                            else
                                li.Selected = false;
                    }
                } // not postback        
            }
            else
            {
                this.id = Convert.ToInt32(string_bugid);

                this.submit_button.Value = "Update";
                Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - Update";
                this.my_header.InnerText = Page.Title;

                if (IsPostBack)
                {
                    if (!validate())
                    {
                        this.msg.InnerHtml = this.err_text;
                    }
                    else
                    {
                        var result = update_bug();
                        if (result != "")
                            this.msg.InnerHtml = this.err_text;
                        else
                            Response.Redirect("mbugs.aspx");
                    }
                }

                var dr = Bug.get_bug_datarow(this.id, this.security);

                if (dr == null)
                {
                    Response.Write("Not found");
                    Response.End();
                    return;
                }

                Page.Title += " #" + string_bugid;
                this.my_header.InnerText = Page.Title;

                this.created_by.InnerText = Convert.ToString(dr["reporter"]);
                this.short_desc.Value = Convert.ToString(dr["short_desc"]);

                // load dropdowns
                load_dropdowns(this.security.user);

                // project
                foreach (ListItem li in this.project.Items)
                    if (Convert.ToInt32(li.Value) == (int) dr["project"])
                        li.Selected = true;
                    else
                        li.Selected = false;

                // status
                foreach (ListItem li in this.status.Items)
                    if (Convert.ToInt32(li.Value) == (int) dr["status"])
                        li.Selected = true;
                    else
                        li.Selected = false;

                // status
                foreach (ListItem li in this.assigned_to.Items)
                    if (Convert.ToInt32(li.Value) == (int) dr["assigned_to_user"])
                        li.Selected = true;
                    else
                        li.Selected = false;

                // Posts
                this.permission_level = (int) dr["pu_permission_level"];
                this.ds_posts = PrintBug.get_bug_posts(this.id, this.security.user.external_user, true);

                // save current values in previous, so that later we can write the audit trail when things change
                this.prev_short_desc.Value = (string) dr["short_desc"];
                this.prev_assigned_to.Value = Convert.ToString((int) dr["assigned_to_user"]);
                this.prev_assigned_to_username.Value = Convert.ToString(dr["assigned_to_username"]);
                this.prev_status.Value = Convert.ToString((int) dr["status"]);
            }
        }

        public void load_dropdowns(User user)
        {
            // only show projects where user has permissions
            // 0
            var sql = @"/* drop downs */ select pj_id, pj_name
		from projects
		left outer join project_user_xref on pj_id = pu_project
		and pu_user = $us
		where pj_active = 1
		and isnull(pu_permission_level,$dpl) not in (0, 1)
		order by pj_name;";

            sql = sql.Replace("$us", Convert.ToString(this.security.user.usid));
            sql = sql.Replace("$dpl", Util.get_setting("DefaultPermissionLevel", "2"));

            //1
            sql += "\nselect us_id, us_username from users order by us_username;";

            //2
            sql += "\nselect st_id, st_name from statuses order by st_sort_seq, st_name;";

            // do a batch of sql statements
            var ds_dropdowns = DbUtil.get_dataset(sql);

            this.project.DataSource = ds_dropdowns.Tables[0];
            this.project.DataTextField = "pj_name";
            this.project.DataValueField = "pj_id";
            this.project.DataBind();
            this.project.Items.Insert(0, new ListItem("[not assigned]", "0"));

            this.assigned_to.DataSource = ds_dropdowns.Tables[1];
            this.assigned_to.DataTextField = "us_username";
            this.assigned_to.DataValueField = "us_id";
            this.assigned_to.DataBind();
            this.assigned_to.Items.Insert(0, new ListItem("[not assigned]", "0"));

            this.status.DataSource = ds_dropdowns.Tables[2];
            this.status.DataTextField = "st_name";
            this.status.DataValueField = "st_id";
            this.status.DataBind();
            this.status.Items.Insert(0, new ListItem("[no status]", "0"));
        }

        public string update_bug()
        {
            this.status_changed = false;
            this.assigned_to_changed = false;

            this.sql = @"update bugs set
				bg_short_desc = N'$sd$',
                        bg_project = $pj$,
						bg_assigned_to_user = $au$,
						bg_status = $st$,
						bg_last_updated_user = $lu$,
						bg_last_updated_date = getdate()
						where bg_id = $id$";

            this.sql = this.sql.Replace("$pj$", this.project.SelectedItem.Value);
            this.sql = this.sql.Replace("$au$", this.assigned_to.SelectedItem.Value);
            this.sql = this.sql.Replace("$st$", this.status.SelectedItem.Value);
            this.sql = this.sql.Replace("$lu$", Convert.ToString(this.security.user.usid));
            this.sql = this.sql.Replace("$sd$", this.short_desc.Value.Replace("'", "''"));
            this.sql = this.sql.Replace("$id$", Convert.ToString(this.id));

            DbUtil.execute_nonquery(this.sql);

            var bug_fields_have_changed = record_changes();

            var comment_text = HttpUtility.HtmlDecode(this.comment.Value);

            var bugpost_fields_have_changed = Bug.insert_comment(this.id, this.security.user.usid,
                                                  comment_text,
                                                  comment_text,
                                                  null, // from
                                                  null, // cc
                                                  "text/plain",
                                                  false) != 0; // internal only

            if (bug_fields_have_changed || bugpost_fields_have_changed)
                Bug.send_notifications(Bug.UPDATE, this.id, this.security, 0, this.status_changed,
                    this.assigned_to_changed,
                    0); // Convert.ToInt32(assigned_to.SelectedItem.Value));

            this.comment.Value = "";

            return "";
        }

        // returns true if there was a change
        public bool record_changes()
        {
            var base_sql = @"
		insert into bug_posts
		(bp_bug, bp_user, bp_date, bp_comment, bp_type)
		values($id, $us, getdate(), N'$3', 'update')";

            base_sql = base_sql.Replace("$id", Convert.ToString(this.id));
            base_sql = base_sql.Replace("$us", Convert.ToString(this.security.user.usid));

            string from;
            this.sql = "";

            var do_update = false;

            if (this.prev_short_desc.Value != this.short_desc.Value)
            {
                do_update = true;
                this.sql += base_sql.Replace(
                    "$3",
                    "changed desc from \""
                    + this.prev_short_desc.Value.Replace("'", "''") + "\" to \""
                    + this.short_desc.Value.Replace("'", "''") + "\"");

                this.prev_short_desc.Value = this.short_desc.Value;
            }

            if (this.project.SelectedItem.Value != this.prev_project.Value)
            {
                // The "from" might not be in the dropdown anymore
                //from = get_dropdown_text_from_value(project, prev_project.Value);

                do_update = true;
                this.sql += base_sql.Replace(
                    "$3",
                    "changed project from \""
                    + this.prev_project_name.Value.Replace("'", "''") + "\" to \""
                    + this.project.SelectedItem.Text.Replace("'", "''") + "\"");

                this.prev_project.Value = this.project.SelectedItem.Value;
                this.prev_project_name.Value = this.project.SelectedItem.Text;
            }

            if (this.prev_assigned_to.Value != this.assigned_to.SelectedItem.Value)
            {
                this.assigned_to_changed = true; // for notifications

                do_update = true;
                this.sql += base_sql.Replace(
                    "$3",
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
                    "$3",
                    "changed status from \""
                    + from.Replace("'", "''") + "\" to \""
                    + this.status.SelectedItem.Text.Replace("'", "''") + "\"");

                this.prev_status.Value = this.status.SelectedItem.Value;
            }

            if (do_update
                && Util.get_setting("TrackBugHistory", "1") == "1") // you might not want the debris to grow
                DbUtil.execute_nonquery(this.sql);

            // return true if something did change
            return do_update;
        }

        public string insert_bug()
        {
            var comment_text = HttpUtility.HtmlDecode(this.comment.Value);

            var new_ids = Bug.insert_bug(this.short_desc.Value, this.security,
                "", //tags.Value,
                Convert.ToInt32(this.project.SelectedItem.Value),
                0, //Convert.ToInt32(org.SelectedItem.Value),
                0, //Convert.ToInt32(category.SelectedItem.Value),
                0, //Convert.ToInt32(priority.SelectedItem.Value),
                Convert.ToInt32(this.status.SelectedItem.Value),
                Convert.ToInt32(this.assigned_to.SelectedItem.Value),
                0, // Convert.ToInt32(udf.SelectedItem.Value),
                "",
                "",
                "",
                comment_text,
                comment_text,
                null, // from
                null, // cc
                "text/plain", // commentType,
                false, // internal_only.Checked,
                null, // hash_custom_cols,
                true); // send notifications

            return "";
        }

        public bool validate()
        {
            var is_valid = true;

            if (this.short_desc.Value == "")
            {
                is_valid = false;
                this.err_text += "Description is required.<br>";
            }

            return is_valid;
        }

        /// ////
        public string get_dropdown_text_from_value(DropDownList dropdown, string value)
        {
            foreach (ListItem li in dropdown.Items)
                if (li.Value == value)
                    return li.Text;

            return dropdown.Items[0].Text;
        }
    }
}
/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Bugs
{
    using System;
    using System.Data;
    using System.Web;
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using Core;

    public partial class MobileEdit : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public ISecurity Security { get; set; }

        public bool AssignedToChanged;
        protected DataSet DsPosts { get; set; }
        public string ErrText;
        public int Id;

        public int PermissionLevel;
        protected string Sql {get; set; }
        public bool StatusChanged;

        //SortedDictionary<string, string> hash_custom_cols = new SortedDictionary<string, string>();
        //SortedDictionary<string, string> hash_prev_custom_cols = new SortedDictionary<string, string>();

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.AnyUserOk);

            if (!ApplicationSettings.EnableMobile)
            {
                Response.Write("BugTracker.NET EnableMobile is not set to 1 in Web.config");
                Response.End();
            }

            this.msg.InnerText = "";
            this.ErrText = "";

            var stringBugid = Request["id"];

            if (stringBugid == null || stringBugid == "" || stringBugid == "0")
            {
                this.Id = 0;

                this.submit_button.Value = "Create";
                Page.Title = $"{ApplicationSettings.AppTitle} - Create";
                this.my_header.InnerText = Page.Title;

                if (IsPostBack)
                {
                    if (!validate())
                    {
                        this.msg.InnerHtml = this.ErrText;
                    }
                    else
                    {
                        var result = insert_bug(Security);
                        if (result != "")
                            this.msg.InnerHtml = this.ErrText;
                        else
                            Response.Redirect("~/Bugs/MobileList.aspx");
                    }
                }
                else
                {
                    load_dropdowns(Security.User, Security);

                    this.Sql = "\nselect top 1 pj_id from projects where pj_default = 1 order by pj_name;"; // 0
                    this.Sql += "\nselect top 1 st_id from statuses where st_default = 1 order by st_name;"; // 1

                    var dsDefaults = DbUtil.GetDataSet(this.Sql);
                    var dtProjectDefault = dsDefaults.Tables[0];
                    var dtStatusDefault = dsDefaults.Tables[1];

                    string defaultValue;

                    // status
                    if (dsDefaults.Tables[1].Rows.Count > 0)
                        defaultValue = Convert.ToString((int) dtStatusDefault.Rows[0][0]);
                    else
                        defaultValue = "0";
                    foreach (ListItem li in this.status.Items)
                        if (li.Value == defaultValue)
                            li.Selected = true;
                        else
                            li.Selected = false;

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
                        if (dtProjectDefault.Rows.Count > 0)
                            defaultValue = Convert.ToString((int) dtProjectDefault.Rows[0][0]);
                        else
                            defaultValue = "0";

                        foreach (ListItem li in this.project.Items)
                            if (li.Value == defaultValue)
                                li.Selected = true;
                            else
                                li.Selected = false;
                    }
                } // not postback        
            }
            else
            {
                this.Id = Convert.ToInt32(stringBugid);

                this.submit_button.Value = "Update";
                Page.Title = $"{ApplicationSettings.AppTitle} - Update";
                this.my_header.InnerText = Page.Title;

                if (IsPostBack)
                {
                    if (!validate())
                    {
                        this.msg.InnerHtml = this.ErrText;
                    }
                    else
                    {
                        var result = update_bug(Security);
                        if (result != "")
                            this.msg.InnerHtml = this.ErrText;
                        else
                            Response.Redirect("~/Bugs/MobileList.aspx");
                    }
                }

                var dr = Bug.GetBugDataRow(this.Id, Security);

                if (dr == null)
                {
                    Response.Write("Not found");
                    Response.End();
                    return;
                }

                Page.Title += " #" + stringBugid;
                this.my_header.InnerText = Page.Title;

                this.created_by.InnerText = Convert.ToString(dr["reporter"]);
                this.short_desc.Value = Convert.ToString(dr["short_desc"]);

                // load dropdowns
                load_dropdowns(Security.User, Security);

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
                this.PermissionLevel = (int) dr["pu_permission_level"];
                this.DsPosts = PrintBug.GetBugPosts(this.Id, Security.User.ExternalUser, true);

                // save current values in previous, so that later we can write the audit trail when things change
                this.prev_short_desc.Value = (string) dr["short_desc"];
                this.prev_assigned_to.Value = Convert.ToString((int) dr["assigned_to_user"]);
                this.prev_assigned_to_username.Value = Convert.ToString(dr["assigned_to_username"]);
                this.prev_status.Value = Convert.ToString((int) dr["status"]);
            }
        }

        public void load_dropdowns(User user, ISecurity security)
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

            sql = sql.Replace("$us", Convert.ToString(Security.User.Usid));
            sql = sql.Replace("$dpl", ApplicationSettings.DefaultPermissionLevel.ToString());

            //1
            sql += "\nselect us_id, us_username from users order by us_username;";

            //2
            sql += "\nselect st_id, st_name from statuses order by st_sort_seq, st_name;";

            // do a batch of sql statements
            var dsDropdowns = DbUtil.GetDataSet(sql);

            this.project.DataSource = dsDropdowns.Tables[0];
            this.project.DataTextField = "pj_name";
            this.project.DataValueField = "pj_id";
            this.project.DataBind();
            this.project.Items.Insert(0, new ListItem("[not assigned]", "0"));

            this.assigned_to.DataSource = dsDropdowns.Tables[1];
            this.assigned_to.DataTextField = "us_username";
            this.assigned_to.DataValueField = "us_id";
            this.assigned_to.DataBind();
            this.assigned_to.Items.Insert(0, new ListItem("[not assigned]", "0"));

            this.status.DataSource = dsDropdowns.Tables[2];
            this.status.DataTextField = "st_name";
            this.status.DataValueField = "st_id";
            this.status.DataBind();
            this.status.Items.Insert(0, new ListItem("[no status]", "0"));
        }

        public string update_bug(ISecurity security)
        {
            this.StatusChanged = false;
            this.AssignedToChanged = false;

            this.Sql = @"update bugs set
                bg_short_desc = N'$sd$',
                        bg_project = $pj$,
                        bg_assigned_to_user = $au$,
                        bg_status = $st$,
                        bg_last_updated_user = $lu$,
                        bg_last_updated_date = getdate()
                        where bg_id = $id$";

            this.Sql = this.Sql.Replace("$pj$", this.project.SelectedItem.Value);
            this.Sql = this.Sql.Replace("$au$", this.assigned_to.SelectedItem.Value);
            this.Sql = this.Sql.Replace("$st$", this.status.SelectedItem.Value);
            this.Sql = this.Sql.Replace("$lu$", Convert.ToString(Security.User.Usid));
            this.Sql = this.Sql.Replace("$sd$", this.short_desc.Value.Replace("'", "''"));
            this.Sql = this.Sql.Replace("$id$", Convert.ToString(this.Id));

            DbUtil.ExecuteNonQuery(this.Sql);

            var bugFieldsHaveChanged = record_changes(security);

            var commentText = HttpUtility.HtmlDecode(this.comment.Value);

            var bugpostFieldsHaveChanged = Bug.InsertComment(this.Id, Security.User.Usid,
                                                  commentText,
                                                  commentText,
                                                  null, // from
                                                  null, // cc
                                                  "text/plain",
                                                  false) != 0; // internal only

            if (bugFieldsHaveChanged || bugpostFieldsHaveChanged)
                Bug.SendNotifications(Bug.Update, this.Id, security, 0, this.StatusChanged,
                    this.AssignedToChanged,
                    0); // Convert.ToInt32(assigned_to.SelectedItem.Value));

            this.comment.Value = "";

            return "";
        }

        // returns true if there was a change
        public bool record_changes(ISecurity security)
        {
            var baseSql = @"
        insert into bug_posts
        (bp_bug, bp_user, bp_date, bp_comment, bp_type)
        values($id, $us, getdate(), N'$3', 'update')";

            baseSql = baseSql.Replace("$id", Convert.ToString(this.Id));
            baseSql = baseSql.Replace("$us", Convert.ToString(Security.User.Usid));

            string from;
            this.Sql = "";

            var doUpdate = false;

            if (this.prev_short_desc.Value != this.short_desc.Value)
            {
                doUpdate = true;
                this.Sql += baseSql.Replace(
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

                doUpdate = true;
                this.Sql += baseSql.Replace(
                    "$3",
                    "changed project from \""
                    + this.prev_project_name.Value.Replace("'", "''") + "\" to \""
                    + this.project.SelectedItem.Text.Replace("'", "''") + "\"");

                this.prev_project.Value = this.project.SelectedItem.Value;
                this.prev_project_name.Value = this.project.SelectedItem.Text;
            }

            if (this.prev_assigned_to.Value != this.assigned_to.SelectedItem.Value)
            {
                this.AssignedToChanged = true; // for notifications

                doUpdate = true;
                this.Sql += baseSql.Replace(
                    "$3",
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
                this.Sql += baseSql.Replace(
                    "$3",
                    "changed status from \""
                    + from.Replace("'", "''") + "\" to \""
                    + this.status.SelectedItem.Text.Replace("'", "''") + "\"");

                this.prev_status.Value = this.status.SelectedItem.Value;
            }

            if (doUpdate
                && ApplicationSettings.TrackBugHistory) // you might not want the debris to grow
                DbUtil.ExecuteNonQuery(this.Sql);

            // return true if something did change
            return doUpdate;
        }

        public string insert_bug(ISecurity security)
        {
            var commentText = HttpUtility.HtmlDecode(this.comment.Value);

            var newIds = Bug.InsertBug(this.short_desc.Value, security,
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
                commentText,
                commentText,
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
            var isValid = true;

            if (this.short_desc.Value == "")
            {
                isValid = false;
                this.ErrText += "Description is required.<br>";
            }

            return isValid;
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
/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Attachments
{
    using System;
    using System.Web.UI;
    using Core;

    public partial class Edit : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public ISecurity Security { get; set; }

        public int Bugid;
        public int Id;
        public string Sql;

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.AnyUserOkExceptGuest);

            MainMenu.SelectedItem = ApplicationSettings.PluralBugLabel;

            if (Security.User.IsAdmin || Security.User.CanEditAndDeletePosts)
            {
                //
            }
            else
            {
                Response.Write("You are not allowed to use this page.");
                Response.End();
            }

            Page.Title = $"{ApplicationSettings.AppTitle} - edit attachment";

            this.msg.InnerText = "";

            var var = Request.QueryString["id"];
            this.Id = Convert.ToInt32(var);

            var = Request.QueryString["bug_id"];
            this.Bugid = Convert.ToInt32(var);

            var permissionLevel = Bug.GetBugPermissionLevel(this.Bugid, Security);
            if (permissionLevel != SecurityPermissionLevel.PermissionAll)
            {
                Response.Write("You are not allowed to edit this item");
                Response.End();
            }

            if (Security.User.ExternalUser || !ApplicationSettings.EnableInternalOnlyPosts)
            {
                this.internal_only.Visible = false;
                this.internal_only_label.Visible = false;
            }

            if (!IsPostBack)
            {
                // Get this entry's data from the db and fill in the form

                this.Sql = @"select bp_comment, bp_file, bp_hidden_from_external_users from bug_posts where bp_id = $1";
                this.Sql = this.Sql.Replace("$1", Convert.ToString(this.Id));
                var dr = DbUtil.GetDataRow(this.Sql);

                // Fill in this form
                this.desc.Value = (string) dr["bp_comment"];
                this.filename.InnerText = (string) dr["bp_file"];
                this.internal_only.Checked = Convert.ToBoolean((int) dr["bp_hidden_from_external_users"]);
            }
            else
            {
                on_update(Security);
            }
        }

        public bool validate()
        {
            var good = true;

            return good;
        }

        public void on_update(ISecurity security)
        {
            var good = validate();

            if (good)
            {
                this.Sql = @"update bug_posts set
            bp_comment = N'$1',
            bp_hidden_from_external_users = $internal
            where bp_id = $3";

                this.Sql = this.Sql.Replace("$3", Convert.ToString(this.Id));
                this.Sql = this.Sql.Replace("$1", this.desc.Value.Replace("'", "''"));
                this.Sql = this.Sql.Replace("$internal", Util.BoolToString(this.internal_only.Checked));

                DbUtil.ExecuteNonQuery(this.Sql);

                if (!this.internal_only.Checked) Bug.SendNotifications(Bug.Update, this.Bugid, security);

                Response.Redirect($"~/Bugs/Edit.aspx?id={this.Bugid}");
            }
            else
            {
                this.msg.InnerText = "Attachment was not updated.";
            }
        }
    }
}
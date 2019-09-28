/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Administration.Priorities
{
    using System;
    using System.Collections.Generic;
    using System.Web.UI;
    using Core;
    using Core.Administration;

    public partial class Edit : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public IPriorityService PriorityService { get; set; }

        protected void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            var security = new Security();

            security.CheckSecurity(Security.MustBeAdmin);

            MainMenu.Security = security;
            MainMenu.SelectedItem = "admin";

            int.TryParse(Request.QueryString["id"], out var id);

            this.msg.InnerText = string.Empty;

            if (IsPostBack)
            {
                OnUpdate(id);
            }
            else
            {
                Page.Title = $"{ApplicationSettings.AppTitle} - edit priority";

                // add or edit?
                if (id == 0)
                {
                    this.sub.Value = "Create";
                }
                else
                {
                    this.sub.Value = "Update";

                    // Get this entry's data from the db and fill in the form
                    var dataRow = PriorityService.LoadOne(id);

                    // Fill in this form
                    this.name.Value = dataRow.Name;
                    this.sortSeq.Value = Convert.ToString(dataRow.SortSequence);
                    this.color.Value = dataRow.BackgroundColor;
                    this.style.Value = dataRow.Style;
                    this.defaultSelection.Checked = Convert.ToBoolean(dataRow.Default);
                }
            }
        }

        private void OnUpdate(int id)
        {
            var good = ValidateForm();

            if (good)
            {
                var parameters = new Dictionary<string, string>
                {
                    { "$id", Convert.ToString(id)},
                    { "$na", this.name.Value.Replace("'", "''")},
                    { "$ss", this.sortSeq.Value},
                    { "$co", this.color.Value.Replace("'", "''")},
                    { "$st", this.style.Value.Replace("'", "''")},
                    { "$df", Util.BoolToString(this.defaultSelection.Checked)},
                };

                if (id == 0) // insert new
                {
                    PriorityService.Create(parameters);
                }
                else // edit existing
                {
                    PriorityService.Update(parameters);
                }

                Response.Redirect("~/Administration/Priorities/List.aspx");
            }
            else
            {
                if (id == 0) // insert new
                {
                    this.msg.InnerText = "Priority was not created.";
                }
                else // edit existing
                {
                    this.msg.InnerText = "Priority was not updated.";
                }
            }
        }

        private bool ValidateForm()
        {
            var good = true;

            if (this.name.Value == string.Empty)
            {
                good = false;
                this.name_err.InnerText = "Description is required.";
            }
            else
            {
                this.name_err.InnerText = string.Empty;
            }

            if (this.sortSeq.Value == string.Empty)
            {
                good = false;
                this.sortSeqErr.InnerText = "Sort Sequence is required.";
            }
            else
            {
                this.sortSeqErr.InnerText = string.Empty;
            }

            if (!Util.IsInt(this.sortSeq.Value))
            {
                good = false;
                this.sortSeqErr.InnerText = "Sort Sequence must be an integer.";
            }
            else
            {
                this.sortSeqErr.InnerText = string.Empty;
            }

            if (this.color.Value == string.Empty)
            {
                good = false;
                this.color_err.InnerText = "Background Color in #FFFFFF format is required.";
            }
            else
            {
                this.color_err.InnerText = "";
            }

            return good;
        }
    }
}
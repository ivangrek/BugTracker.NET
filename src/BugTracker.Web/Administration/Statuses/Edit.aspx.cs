/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Administration.Statuses
{
    using System;
    using System.Collections.Generic;
    using System.Web;
    using System.Web.UI;
    using Core;
    using Core.Administration;

    public partial class Edit : Page
    {
        protected Security Security { get; set; }

        protected void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security = new Security();
            Security.CheckSecurity(HttpContext.Current, Security.MustBeAdmin);

            int.TryParse(Request.QueryString["id"], out var id);

            this.msg.InnerText = string.Empty;

            if (IsPostBack)
            {
                OnUpdate(id);
            }
            else
            {
                Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - edit status";

                // add or edit?
                if (id == 0)
                {
                    this.sub.Value = "Create";
                }
                else
                {
                    this.sub.Value = "Update";
                    Validate();
                    // Get this entry's data from the db and fill in the form
                    var dataRow = StatusService.LoadOne(id);

                    // Fill in this form
                    this.name.Value = (string)dataRow["st_name"];
                    this.sortSeq.Value = Convert.ToString((int)dataRow["st_sort_seq"]);
                    this.style.Value = (string)dataRow["st_style"];
                    this.defaultSelection.Checked = Convert.ToBoolean((int)dataRow["st_default"]);
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
                    { "$st", this.style.Value.Replace("'", "''")},
                    { "$df", Util.BoolToString(this.defaultSelection.Checked)},
                };

                if (id == 0) // insert new
                {
                    StatusService.Create(parameters);
                }
                else // edit existing
                {
                    StatusService.Update(parameters);
                }

                Server.Transfer("~/Administration/Statuses/List.aspx");
            }
            else
            {
                if (id == 0) // insert new
                {
                    this.msg.InnerText = "Status was not created.";
                }
                else // edit existing
                {
                    this.msg.InnerText = "Status was not updated.";
                }
            }
        }

        private bool ValidateForm()
        {
            var good = true;

            if (this.name.Value == string.Empty)
            {
                good = false;
                this.nameErr.InnerText = "Description is required.";
            }
            else
            {
                this.nameErr.InnerText = string.Empty;
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

            return good;
        }
    }
}
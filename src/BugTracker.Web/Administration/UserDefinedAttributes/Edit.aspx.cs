/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Administration.UserDefinedAttributes
{
    using System;
    using System.Collections.Generic;
    using System.Web;
    using System.Web.UI;
    using Core;
    using Core.Administration;
    using Core.Persistence;

    public partial class Edit : Page
    {
        private readonly IUserDefinedAttributeService userDefinedAttributeService = new UserDefinedAttributeService(new ApplicationContext());

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
                Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - edit user defined attribute value";

                // add or edit?
                if (id == 0)
                {
                    this.sub.Value = "Create";
                }
                else
                {
                    this.sub.Value = "Update";

                    // Get this entry's data from the db and fill in the form
                    var dataRow = this.userDefinedAttributeService.LoadOne(id);

                    // Fill in this form
                    this.name.Value = dataRow.Name;
                    this.sortSeq.Value = Convert.ToString(dataRow.SortSequence);
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
                    { "$df", Util.BoolToString(this.defaultSelection.Checked)},
                };

                if (id == 0) // insert new
                {
                    this.userDefinedAttributeService.Create(parameters);
                }
                else // edit existing
                {
                    this.userDefinedAttributeService.Update(parameters);
                }

                Server.Transfer("~/Administration/UserDefinedAttributes/List.aspx");
            }
            else
            {
                if (id == 0) // insert new
                {
                    this.msg.InnerText = "User defined attribute value was not created.";
                }
                else // edit existing
                {
                    this.msg.InnerText = "User defined attribute value was not updated.";
                }
            }
        }

        public bool ValidateForm()
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
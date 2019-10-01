/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Administration.CustomFields
{
    using System;
    using System.Web.UI;
    using Core;

    public partial class Edit : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public ISecurity Security { get; set; }

        public int Id;
        public string Sql;

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.MustBeAdmin);

            MainMenu.SelectedItem = ApplicationSettings.PluralBugLabel;

            Page.Title = $"{ApplicationSettings.AppTitle} - edit custom column metadata";

            this.msg.InnerText = "";

            this.Id = Convert.ToInt32(Util.SanitizeInteger(Request["id"]));

            if (!IsPostBack)
            {
                // Get this entry's data from the db and fill in the form

                this.Sql = @"
select sc.name,
isnull(ccm_dropdown_vals,'') [vals],
isnull(ccm_dropdown_type,'') [dropdown_type],
isnull(ccm_sort_seq, sc.colorder) [column order],
mm.text [default value], dflts.name [default name]
from syscolumns sc
inner join sysobjects so on sc.id = so.id
left outer join custom_col_metadata ccm on ccm_colorder = sc.colorder
left outer join syscomments mm on sc.cdefault = mm.id
left outer join sysobjects dflts on dflts.id = mm.id
where so.name = 'bugs'
and sc.colorder = $co";

                this.Sql = this.Sql.Replace("$co", Convert.ToString(this.Id));
                var dr = DbUtil.GetDataRow(this.Sql);

                this.name.InnerText = (string) dr["name"];
                this.dropdown_type.Value = Convert.ToString(dr["dropdown_type"]);

                if (this.dropdown_type.Value == "normal")
                {
                    // show the dropdown vals
                }
                else
                {
                    this.vals.Visible = false;
                    this.vals_label.Visible = false;
                    //vals_explanation.Visible = false;
                }

                // Fill in this form
                this.vals.Value = (string) dr["vals"];
                this.sort_seq.Value = Convert.ToString(dr["column order"]);
                this.default_value.Value = Convert.ToString(dr["default value"]);
                this.hidden_default_value.Value = this.default_value.Value; // to test if it changed
                this.hidden_default_name.Value = Convert.ToString(dr["default name"]);
            }
            else
            {
                on_update();
            }
        }

        public bool validate()
        {
            var good = true;

            this.sort_seq_err.InnerText = "";
            this.vals_err.InnerText = "";

            if (this.sort_seq.Value == "")
            {
                good = false;
                this.sort_seq_err.InnerText = "Sort Sequence is required.";
            }

            if (!Util.IsInt(this.sort_seq.Value))
            {
                good = false;
                this.sort_seq_err.InnerText = "Sort Sequence must be an integer.";
            }

            if (this.dropdown_type.Value == "normal")
            {
                if (this.vals.Value == "")
                {
                    good = false;
                    this.vals_err.InnerText = "Dropdown values are required for dropdown type of \"normal\".";
                }
                else
                {
                    var valsErrorString = Util.ValidateDropdownValues(this.vals.Value);
                    if (!string.IsNullOrEmpty(valsErrorString))
                    {
                        good = false;
                        this.vals_err.InnerText = valsErrorString;
                    }
                }
            }

            return good;
        }

        public void on_update()
        {
            var good = validate();

            if (good)
            {
                this.Sql = @"declare @count int
			select @count = count(1) from custom_col_metadata
			where ccm_colorder = $co

			if @count = 0
				insert into custom_col_metadata
				(ccm_colorder, ccm_dropdown_vals, ccm_sort_seq, ccm_dropdown_type)
				values($co, N'$v', $ss, '$dt')
			else
				update custom_col_metadata
				set ccm_dropdown_vals = N'$v',
				ccm_sort_seq = $ss
				where ccm_colorder = $co";

                this.Sql = this.Sql.Replace("$co", Convert.ToString(this.Id));
                this.Sql = this.Sql.Replace("$v", this.vals.Value.Replace("'", "''"));
                this.Sql = this.Sql.Replace("$ss", this.sort_seq.Value);

                DbUtil.ExecuteNonQuery(this.Sql);
                Application["custom_columns_dataset"] = null;

                if (this.default_value.Value != this.hidden_default_value.Value)
                {
                    if (this.hidden_default_name.Value != "")
                    {
                        this.Sql = "alter table bugs drop constraint [" +
                                   this.hidden_default_name.Value.Replace("'", "''") + "]";
                        DbUtil.ExecuteNonQuery(this.Sql);
                        Application["custom_columns_dataset"] = null;
                    }

                    if (this.default_value.Value != "")
                    {
                        this.Sql = "alter table bugs add constraint [" + Guid.NewGuid() + "] default " +
                                   this.default_value.Value.Replace("'", "''") + " for [" + this.name.InnerText + "]";
                        DbUtil.ExecuteNonQuery(this.Sql);
                        Application["custom_columns_dataset"] = null;
                    }
                }

                Response.Redirect("~/Administration/CustomFields/List.aspx");
            }
            else
            {
                this.msg.InnerText = "dropdown values were not updated.";
            }
        }
    }
}
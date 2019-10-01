/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Administration.CustomFields
{
    using System;
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using BugTracker.Web.Core.Controls;
    using Core;

    public partial class Add : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public ISecurity Security { get; set; }

        public string Sql;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.MustBeAdmin);

            MainMenu.SelectedItem = MainMenuSections.Administration;

            Page.Title = $"{ApplicationSettings.AppTitle} - custom field";

            this.msg.InnerText = "";

            if (!IsPostBack)
            {
                this.datatype.Items.Insert(0, new ListItem("char", "char"));
                this.datatype.Items.Insert(0, new ListItem("datetime", "datetime"));
                this.datatype.Items.Insert(0, new ListItem("decimal", "decimal"));
                this.datatype.Items.Insert(0, new ListItem("int", "int"));
                this.datatype.Items.Insert(0, new ListItem("nchar", "nchar"));
                this.datatype.Items.Insert(0, new ListItem("nvarchar", "nvarchar"));
                this.datatype.Items.Insert(0, new ListItem("varchar", "varchar"));

                this.dropdown_type.Items.Insert(0, new ListItem("not a dropdown", ""));
                this.dropdown_type.Items.Insert(1, new ListItem("normal", "normal"));
                this.dropdown_type.Items.Insert(2, new ListItem("users", "users"));

                this.sort_seq.Value = "1";
            }
            else
            {
                on_update();
            }
        }

        public bool validate()
        {
            this.name_err.InnerText = "";
            this.length_err.InnerText = "";
            this.sort_seq_err.InnerText = "";
            this.default_err.InnerText = "";
            this.vals_err.InnerText = "";
            this.datatype_err.InnerText = "";
            this.required_err.InnerText = "";

            var good = true;

            if (string.IsNullOrEmpty(this.name.Value))
            {
                good = false;
                this.name_err.InnerText = "Field name is required.";
            }
            else
            {
                if (this.name.Value.ToLower() == "url")
                {
                    good = false;
                    this.name_err.InnerText = "Field name of \"URL\" causes problems with ASP.NET.";
                }
                else if (this.name.Value.Contains("'")
                         || this.name.Value.Contains("\\")
                         || this.name.Value.Contains("/")
                         || this.name.Value.Contains("\"")
                         || this.name.Value.Contains("<")
                         || this.name.Value.Contains(">"))
                {
                    good = false;
                    this.name_err.InnerText = "Some special characters like quotes, slashes are not allowed.";
                }
            }

            if (string.IsNullOrEmpty(this.length.Value))
            {
                if (this.datatype.SelectedItem.Value == "int"
                    || this.datatype.SelectedItem.Value == "datetime")
                {
                    // ok
                }
                else
                {
                    good = false;
                    this.length_err.InnerText = "Length or Precision is required for this datatype.";
                }
            }
            else
            {
                if (this.datatype.SelectedItem.Value == "int"
                    || this.datatype.SelectedItem.Value == "datetime")
                {
                    good = false;
                    this.length_err.InnerText = "Length or Precision not allowed for this datatype.";
                }
            }

            if (this.required.Checked)
            {
                if (string.IsNullOrEmpty(this.default_text.Value))
                {
                    good = false;
                    this.default_err.InnerText = "If \"Required\" is checked, then Default is required.";
                }

                if (this.dropdown_type.SelectedItem.Value != "")
                {
                    good = false;
                    this.required_err.InnerText =
                        "Checking \"Required\" is not compatible with a normal or users dropdown";
                }
            }

            if (this.dropdown_type.SelectedItem.Value == "normal")
            {
                if (string.IsNullOrEmpty(this.vals.Value))
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
                    else
                    {
                        if (this.datatype.SelectedItem.Value == "int"
                            || this.datatype.SelectedItem.Value == "decimal"
                            || this.datatype.SelectedItem.Value == "datetime")
                        {
                            good = false;
                            this.datatype_err.InnerText =
                                "For a normal dropdown datatype must be char, varchar, nchar, or nvarchar.";
                        }
                    }
                }
            }
            else if (this.dropdown_type.SelectedItem.Value == "users")
            {
                if (this.datatype.SelectedItem.Value != "int")
                {
                    good = false;
                    this.datatype_err.InnerText = "For a users dropdown datatype must be int.";
                }
            }

            if (this.dropdown_type.SelectedItem.Value != "normal")
                if (this.vals.Value != "")
                {
                    good = false;
                    this.vals_err.InnerText = "Dropdown values are only used for dropdown of type \"normal\".";
                }

            if (string.IsNullOrEmpty(this.sort_seq.Value))
            {
                good = false;
                this.sort_seq_err.InnerText = "Sort Sequence is required.";
            }
            else
            {
                if (!Util.IsInt(this.sort_seq.Value))
                {
                    good = false;
                    this.sort_seq_err.InnerText = "Sort Sequence must be an integer.";
                }
            }

            return good;
        }

        public void on_update()
        {
            var good = validate();

            if (good)
            {
                this.Sql = @"
alter table orgs add [og_$nm_field_permission_level] int null
alter table bugs add [$nm] $dt $ln $null $df";

                this.Sql = this.Sql.Replace("$nm", this.name.Value);
                this.Sql = this.Sql.Replace("$dt", this.datatype.SelectedItem.Value);

                if (this.length.Value != "")
                {
                    if (this.length.Value.StartsWith("("))
                        this.Sql = this.Sql.Replace("$ln", this.length.Value);
                    else
                        this.Sql = this.Sql.Replace("$ln", "(" + this.length.Value + ")");
                }
                else
                {
                    this.Sql = this.Sql.Replace("$ln", "");
                }

                if (this.default_text.Value != "")
                {
                    if (this.default_text.Value.StartsWith("("))
                        this.Sql = this.Sql.Replace("$df", "DEFAULT " + this.default_text.Value);
                    else
                        this.Sql = this.Sql.Replace("$df", "DEFAULT (" + this.default_text.Value + ")");
                }
                else
                {
                    this.Sql = this.Sql.Replace("$df", "");
                }

                if (this.required.Checked)
                    this.Sql = this.Sql.Replace("$null", "NOT NULL");
                else
                    this.Sql = this.Sql.Replace("$null", "NULL");

                var alterTableWorked = false;
                try
                {
                    DbUtil.ExecuteNonQuery(this.Sql);
                    alterTableWorked = true;
                }
                catch (Exception e2)
                {
                    this.msg.InnerHtml = "The generated SQL was invalid:<br><br>SQL:&nbsp;" + this.Sql +
                                         "<br><br>Error:&nbsp;" + e2.Message;
                    alterTableWorked = false;
                }

                if (alterTableWorked)
                {
                    this.Sql = @"declare @colorder int

                select @colorder = sc.colorder
                from syscolumns sc
                inner join sysobjects so on sc.id = so.id
                where so.name = 'bugs'
                and sc.name = '$nm'

                insert into custom_col_metadata
                (ccm_colorder, ccm_dropdown_vals, ccm_sort_seq, ccm_dropdown_type)
                values(@colorder, N'$v', $ss, '$dt')";

                    this.Sql = this.Sql.Replace("$nm", this.name.Value);
                    this.Sql = this.Sql.Replace("$v", this.vals.Value.Replace("'", "''"));
                    this.Sql = this.Sql.Replace("$ss", this.sort_seq.Value);
                    this.Sql = this.Sql.Replace("$dt", this.dropdown_type.SelectedItem.Value.Replace("'", "''"));

                    DbUtil.ExecuteNonQuery(this.Sql);
                    Application["custom_columns_dataset"] = null;
                    Response.Redirect("~/Administration/CustomFields/List.aspx");
                }
            }
            else
            {
                this.msg.InnerText = "Custom field was not created.";
            }
        }
    }
}
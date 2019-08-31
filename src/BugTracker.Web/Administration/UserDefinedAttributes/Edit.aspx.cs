/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Administration.UserDefinedAttributes
{
    using System;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class Edit : Page
    {
        public int Id;

        public Security Security;
        public string Sql;

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            this.Security = new Security();
            this.Security.CheckSecurity(HttpContext.Current, Security.MustBeAdmin);

            Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "edit user defined attribute value";

            this.msg.InnerText = "";

            var var = Request.QueryString["id"];
            if (var == null)
                this.Id = 0;
            else
                this.Id = Convert.ToInt32(var);

            if (!IsPostBack)
            {
                // add or edit?
                if (this.Id == 0)
                {
                    this.sub.Value = "Create";
                }
                else
                {
                    this.sub.Value = "Update";

                    // Get this entry's data from the db and fill in the form

                    this.Sql =
                        @"select udf_name, udf_sort_seq, udf_default from user_defined_attribute where udf_id = $1";
                    this.Sql = this.Sql.Replace("$1", Convert.ToString(this.Id));
                    var dr = DbUtil.GetDataRow(this.Sql);

                    // Fill in this form
                    this.name.Value = (string) dr[0];
                    this.sort_seq.Value = Convert.ToString((int) dr[1]);
                    this.default_selection.Checked = Convert.ToBoolean((int) dr["udf_default"]);
                }
            }
            else
            {
                on_update();
            }
        }

        public bool validate()
        {
            var good = true;
            if (this.name.Value == "")
            {
                good = false;
                this.name_err.InnerText = "Description is required.";
            }
            else
            {
                this.name_err.InnerText = "";
            }

            if (this.sort_seq.Value == "")
            {
                good = false;
                this.sort_seq_err.InnerText = "Sort Sequence is required.";
            }
            else
            {
                this.sort_seq_err.InnerText = "";
            }

            if (!Util.IsInt(this.sort_seq.Value))
            {
                good = false;
                this.sort_seq_err.InnerText = "Sort Sequence must be an integer.";
            }
            else
            {
                this.sort_seq_err.InnerText = "";
            }

            return good;
        }

        public void on_update()
        {
            var good = validate();

            if (good)
            {
                if (this.Id == 0) // insert new
                {
                    this.Sql =
                        "insert into user_defined_attribute (udf_name, udf_sort_seq, udf_default) values (N'$na', $ss, $df)";
                }
                else // edit existing
                {
                    this.Sql = @"update user_defined_attribute set
                udf_name = N'$na',
                udf_sort_seq = $ss,
                udf_default = $df
                where udf_id = $id";

                    this.Sql = this.Sql.Replace("$id", Convert.ToString(this.Id));
                }

                this.Sql = this.Sql.Replace("$na", this.name.Value.Replace("'", "''"));
                this.Sql = this.Sql.Replace("$ss", this.sort_seq.Value);
                this.Sql = this.Sql.Replace("$df", Util.BoolToString(this.default_selection.Checked));
                DbUtil.ExecuteNonQuery(this.Sql);
                Server.Transfer("~/Administration/UserDefinedAttributes/List.aspx");
            }
            else
            {
                if (this.Id == 0) // insert new
                    this.msg.InnerText = "User defined attribute value was not created.";
                else // edit existing
                    this.msg.InnerText = "User defined attribute value was not updated.";
            }
        }
    }
}
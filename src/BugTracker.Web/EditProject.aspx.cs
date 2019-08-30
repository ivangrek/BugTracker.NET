/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Web;
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using Core;

    public partial class EditProject : Page
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
                                                                        + "edit project";

            this.msg.InnerText = "";

            var var = Request.QueryString["id"];
            if (var == null)
                this.Id = 0;
            else
                this.Id = Convert.ToInt32(var);

            if (!IsPostBack)
            {
                this.default_user.DataSource =
                    DbUtil.GetDataView("select us_id, us_username from users order by us_username");
                this.default_user.DataTextField = "us_username";
                this.default_user.DataValueField = "us_id";
                this.default_user.DataBind();
                this.default_user.Items.Insert(0, new ListItem("", "0"));

                // add or edit?
                if (this.Id == 0)
                {
                    this.sub.Value = "Create";
                    this.active.Checked = true;
                }
                else
                {
                    this.sub.Value = "Update";

                    // Get this entry's data from the db and fill in the form

                    this.Sql = @"select
			pj_name,
			pj_active,
			isnull(pj_default_user,0) [pj_default_user],
			pj_default,
			isnull(pj_auto_assign_default_user,0) [pj_auto_assign_default_user],
			isnull(pj_auto_subscribe_default_user,0) [pj_auto_subscribe_default_user],
			isnull(pj_enable_pop3,0) [pj_enable_pop3],
			isnull(pj_pop3_username,'') [pj_pop3_username],
			isnull(pj_pop3_email_from,'') [pj_pop3_email_from],
			isnull(pj_description,'') [pj_description],
			isnull(pj_enable_custom_dropdown1,0) [pj_enable_custom_dropdown1],
			isnull(pj_enable_custom_dropdown2,0) [pj_enable_custom_dropdown2],
			isnull(pj_enable_custom_dropdown3,0) [pj_enable_custom_dropdown3],
			isnull(pj_custom_dropdown_label1,'') [pj_custom_dropdown_label1],
			isnull(pj_custom_dropdown_label2,'') [pj_custom_dropdown_label2],
			isnull(pj_custom_dropdown_label3,'') [pj_custom_dropdown_label3],
			isnull(pj_custom_dropdown_values1,'') [pj_custom_dropdown_values1],
			isnull(pj_custom_dropdown_values2,'') [pj_custom_dropdown_values2],
			isnull(pj_custom_dropdown_values3,'') [pj_custom_dropdown_values3]
			from projects
			where pj_id = $1";
                    this.Sql = this.Sql.Replace("$1", Convert.ToString(this.Id));
                    var dr = DbUtil.GetDataRow(this.Sql);

                    // Fill in this form
                    this.name.Value = (string) dr["pj_name"];
                    this.active.Checked = Convert.ToBoolean((int) dr["pj_active"]);
                    this.auto_assign.Checked = Convert.ToBoolean((int) dr["pj_auto_assign_default_user"]);
                    this.auto_subscribe.Checked = Convert.ToBoolean((int) dr["pj_auto_subscribe_default_user"]);
                    this.default_selection.Checked = Convert.ToBoolean((int) dr["pj_default"]);
                    this.enable_pop3.Checked = Convert.ToBoolean((int) dr["pj_enable_pop3"]);
                    this.pop3_username.Value = (string) dr["pj_pop3_username"];
                    this.pop3_email_from.Value = (string) dr["pj_pop3_email_from"];

                    this.enable_custom_dropdown1.Checked = Convert.ToBoolean((int) dr["pj_enable_custom_dropdown1"]);
                    this.enable_custom_dropdown2.Checked = Convert.ToBoolean((int) dr["pj_enable_custom_dropdown2"]);
                    this.enable_custom_dropdown3.Checked = Convert.ToBoolean((int) dr["pj_enable_custom_dropdown3"]);

                    this.custom_dropdown_label1.Value = (string) dr["pj_custom_dropdown_label1"];
                    this.custom_dropdown_label2.Value = (string) dr["pj_custom_dropdown_label2"];
                    this.custom_dropdown_label3.Value = (string) dr["pj_custom_dropdown_label3"];

                    this.custom_dropdown_values1.Value = (string) dr["pj_custom_dropdown_values1"];
                    this.custom_dropdown_values2.Value = (string) dr["pj_custom_dropdown_values2"];
                    this.custom_dropdown_values3.Value = (string) dr["pj_custom_dropdown_values3"];

                    this.desc.Value = (string) dr["pj_description"];

                    foreach (ListItem li in this.default_user.Items)
                        if (Convert.ToInt32(li.Value) == (int) dr["pj_default_user"])
                        {
                            li.Selected = true;
                            break;
                        }

                    this.permissions_href.HRef = "EditUserPermissions2.aspx?id=" + Convert.ToString(this.Id)
                                                                                   + "&label=" +
                                                                                   HttpUtility.UrlEncode(
                                                                                       this.name.Value);
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

            var valsErrorString = "";
            var errorsWithCustomDropdowns = false;
            valsErrorString = Util.ValidateDropdownValues(this.custom_dropdown_values1.Value);
            if (!string.IsNullOrEmpty(valsErrorString))
            {
                good = false;
                this.custom_dropdown_values1_err.InnerText = valsErrorString;
                errorsWithCustomDropdowns = true;
            }
            else
            {
                this.custom_dropdown_values1_err.InnerText = "";
            }

            valsErrorString = Util.ValidateDropdownValues(this.custom_dropdown_values2.Value);
            if (!string.IsNullOrEmpty(valsErrorString))
            {
                good = false;
                this.custom_dropdown_values2_err.InnerText = valsErrorString;
                errorsWithCustomDropdowns = true;
            }
            else
            {
                this.custom_dropdown_values2_err.InnerText = "";
            }

            valsErrorString = Util.ValidateDropdownValues(this.custom_dropdown_values3.Value);
            if (!string.IsNullOrEmpty(valsErrorString))
            {
                good = false;
                this.custom_dropdown_values3_err.InnerText = valsErrorString;
                errorsWithCustomDropdowns = true;
            }
            else
            {
                this.custom_dropdown_values3_err.InnerText = "";
            }

            if (errorsWithCustomDropdowns) this.msg.InnerText += "Custom fields have errors.  ";

            return good;
        }

        public void on_update()
        {
            var good = validate();

            if (good)
            {
                if (this.Id == 0) // insert new
                {
                    this.Sql = @"insert into projects
			(pj_name, pj_active, pj_default_user, pj_default, pj_auto_assign_default_user, pj_auto_subscribe_default_user,
			pj_enable_pop3,
			pj_pop3_username,
			pj_pop3_password,
			pj_pop3_email_from,
			pj_description,
			pj_enable_custom_dropdown1,
			pj_enable_custom_dropdown2,
			pj_enable_custom_dropdown3,
			pj_custom_dropdown_label1,
			pj_custom_dropdown_label2,
			pj_custom_dropdown_label3,
			pj_custom_dropdown_values1,
			pj_custom_dropdown_values2,
			pj_custom_dropdown_values3)
			values (N'$name', $active, $defaultuser, $defaultsel, $autoasg, $autosub,
			$enablepop, N'$popuser',N'$poppass',N'$popfrom',
			N'$desc', 
			$ecd1,$ecd2,$ecd3,
			N'$cdl1',N'$cdl2',N'$cdl3',
			N'$cdv1',N'$cdv2',N'$cdv3')";

                    this.Sql = this.Sql.Replace("$poppass", this.pop3_password.Value.Replace("'", "''"));
                }
                else // edit existing
                {
                    this.Sql = @"update projects set
				pj_name = N'$name',
				$POP3_PASSWORD
				pj_active = $active,
				pj_default_user = $defaultuser,
				pj_default = $defaultsel,
				pj_auto_assign_default_user = $autoasg,
				pj_auto_subscribe_default_user = $autosub,
				pj_enable_pop3 = $enablepop,
				pj_pop3_username = N'$popuser',
				pj_pop3_email_from = N'$popfrom',
				pj_description = N'$desc',
				pj_enable_custom_dropdown1 = $ecd1,
				pj_enable_custom_dropdown2 = $ecd2,
				pj_enable_custom_dropdown3 = $ecd3,
				pj_custom_dropdown_label1 = N'$cdl1',
				pj_custom_dropdown_label2 = N'$cdl2',
				pj_custom_dropdown_label3 = N'$cdl3',
				pj_custom_dropdown_values1 = N'$cdv1',
				pj_custom_dropdown_values2 = N'$cdv2',
				pj_custom_dropdown_values3 = N'$cdv3'
				where pj_id = $id";
                    this.Sql = this.Sql.Replace("$id", Convert.ToString(this.Id));

                    if (this.pop3_password.Value != "")
                        this.Sql = this.Sql.Replace("$POP3_PASSWORD",
                            "pj_pop3_password = N'" + this.pop3_password.Value.Replace("'", "''") + "',");
                    else
                        this.Sql = this.Sql.Replace("$POP3_PASSWORD", "");
                }

                this.Sql = this.Sql.Replace("$name", this.name.Value.Replace("'", "''"));
                this.Sql = this.Sql.Replace("$active", Util.BoolToString(this.active.Checked));
                this.Sql = this.Sql.Replace("$defaultuser", this.default_user.SelectedItem.Value);
                this.Sql = this.Sql.Replace("$autoasg", Util.BoolToString(this.auto_assign.Checked));
                this.Sql = this.Sql.Replace("$autosub", Util.BoolToString(this.auto_subscribe.Checked));
                this.Sql = this.Sql.Replace("$defaultsel", Util.BoolToString(this.default_selection.Checked));
                this.Sql = this.Sql.Replace("$enablepop", Util.BoolToString(this.enable_pop3.Checked));
                this.Sql = this.Sql.Replace("$popuser", this.pop3_username.Value.Replace("'", "''"));
                this.Sql = this.Sql.Replace("$popfrom", this.pop3_email_from.Value.Replace("'", "''"));

                this.Sql = this.Sql.Replace("$desc", this.desc.Value.Replace("'", "''"));

                this.Sql = this.Sql.Replace("$ecd1", Util.BoolToString(this.enable_custom_dropdown1.Checked));
                this.Sql = this.Sql.Replace("$ecd2", Util.BoolToString(this.enable_custom_dropdown2.Checked));
                this.Sql = this.Sql.Replace("$ecd3", Util.BoolToString(this.enable_custom_dropdown3.Checked));

                this.Sql = this.Sql.Replace("$cdl1", this.custom_dropdown_label1.Value.Replace("'", "''"));
                this.Sql = this.Sql.Replace("$cdl2", this.custom_dropdown_label2.Value.Replace("'", "''"));
                this.Sql = this.Sql.Replace("$cdl3", this.custom_dropdown_label3.Value.Replace("'", "''"));

                this.Sql = this.Sql.Replace("$cdv1", this.custom_dropdown_values1.Value.Replace("'", "''"));
                this.Sql = this.Sql.Replace("$cdv2", this.custom_dropdown_values2.Value.Replace("'", "''"));
                this.Sql = this.Sql.Replace("$cdv3", this.custom_dropdown_values3.Value.Replace("'", "''"));

                DbUtil.ExecuteNonQuery(this.Sql);
                Server.Transfer("Projects.aspx");
            }
            else
            {
                if (this.Id == 0) // insert new
                    this.msg.InnerText += "Project was not created.";
                else // edit existing
                    this.msg.InnerText += "Project was not updated.";
            }
        }
    }
}
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

    public partial class edit_query : Page
    {
        public int id;

        public Security security;
        public string sql;

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();

            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "edit query";

            this.msg.InnerText = "";

            var var = Request.QueryString["id"];
            if (var == null)
                this.id = 0;
            else
                this.id = Convert.ToInt32(var);

            if (!IsPostBack)
            {
                if (this.security.user.is_admin || this.security.user.can_edit_sql)
                {
                    // these guys can do everything
                    this.vis_everybody.Checked = true;

                    this.sql = @"/* populate org/user dropdowns */
select og_id, og_name from orgs order by og_name;
select us_id, us_username from users order by us_username";

                    var ds_orgs_and_users = DbUtil.get_dataset(this.sql);

                    // forced project dropdown
                    this.org.DataSource = ds_orgs_and_users.Tables[0].DefaultView;
                    this.org.DataTextField = "og_name";
                    this.org.DataValueField = "og_id";
                    this.org.DataBind();
                    this.org.Items.Insert(0, new ListItem("[select org]", "0"));

                    this.user.DataSource = ds_orgs_and_users.Tables[1].DefaultView;
                    this.user.DataTextField = "us_username";
                    this.user.DataValueField = "us_id";
                    this.user.DataBind();
                    this.user.Items.Insert(0, new ListItem("[select user]", "0"));
                }
                else
                {
                    this.sql_text.Visible = false;
                    this.sql_text_label.Visible = false;
                    this.explanation.Visible = false;

                    this.vis_everybody.Enabled = false;
                    this.vis_org.Enabled = false;
                    this.vis_user.Checked = true;
                    this.org.Enabled = false;
                    this.user.Enabled = false;

                    this.org.Visible = false;
                    this.user.Visible = false;
                    this.vis_everybody.Visible = false;
                    this.vis_org.Visible = false;
                    this.vis_user.Visible = false;
                    this.visibility_label.Visible = false;
                }

                // add or edit?
                if (this.id == 0)
                {
                    this.sub.Value = "Create";
                    this.sql_text.Value =
                        HttpUtility.HtmlDecode(Request.Form["sql_text"]); // if coming from search.aspx
                }
                else
                {
                    this.sub.Value = "Update";

                    // Get this entry's data from the db and fill in the form

                    this.sql = @"select
				qu_desc, qu_sql, isnull(qu_user,0) [qu_user], isnull(qu_org,0) [qu_org]
				from queries where qu_id = $1";

                    this.sql = this.sql.Replace("$1", Convert.ToString(this.id));
                    var dr = DbUtil.get_datarow(this.sql);

                    if ((int) dr["qu_user"] != this.security.user.usid)
                    {
                        if (this.security.user.is_admin || this.security.user.can_edit_sql)
                        {
                            // these guys can do everything
                        }
                        else
                        {
                            Response.Write("You are not allowed to edit this query");
                            Response.End();
                        }
                    }

                    // Fill in this form
                    this.desc.Value = (string) dr["qu_desc"];

                    //			if (Util.get_setting("HtmlEncodeSql","0") == "1")
                    //			{
                    //				sql_text.Value = Server.HtmlEncode((string) dr["qu_sql"]);
                    //			}
                    //			else
                    //			{
                    this.sql_text.Value = (string) dr["qu_sql"];
                    //			}

                    if ((int) dr["qu_user"] == 0 && (int) dr["qu_org"] == 0)
                    {
                        this.vis_everybody.Checked = true;
                    }
                    else if ((int) dr["qu_user"] != 0)
                    {
                        this.vis_user.Checked = true;
                        foreach (ListItem li in this.user.Items)
                            if (Convert.ToInt32(li.Value) == (int) dr["qu_user"])
                            {
                                li.Selected = true;
                                break;
                            }
                    }
                    else
                    {
                        this.vis_org.Checked = true;
                        foreach (ListItem li in this.org.Items)
                            if (Convert.ToInt32(li.Value) == (int) dr["qu_org"])
                            {
                                li.Selected = true;
                                break;
                            }
                    }
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

            if (this.desc.Value == "")
            {
                good = false;
                this.desc_err.InnerText = "Description is required.";
            }
            else
            {
                this.desc_err.InnerText = "";
            }

            if (this.security.user.is_admin || this.security.user.can_edit_sql)
            {
                if (this.vis_org.Checked)
                {
                    if (this.org.SelectedIndex < 1)
                    {
                        good = false;
                        this.org_err.InnerText = "You must select a org.";
                    }
                    else
                    {
                        this.org_err.InnerText = "";
                    }
                }
                else if (this.vis_user.Checked)
                {
                    if (this.user.SelectedIndex < 1)
                    {
                        good = false;
                        this.user_err.InnerText = "You must select a user.";
                    }
                    else
                    {
                        this.user_err.InnerText = "";
                    }
                }
                else
                {
                    this.org_err.InnerText = "";
                }
            }

            if (this.id == 0)
            {
                // See if name is already used?
                this.sql = "select count(1) from queries where qu_desc = N'$de'";
                this.sql = this.sql.Replace("$de", this.desc.Value.Replace("'", "''"));
                var query_count = (int) DbUtil.execute_scalar(this.sql);

                if (query_count == 1)
                {
                    this.desc_err.InnerText = "A query with this name already exists.   Choose another name.";
                    this.msg.InnerText = "Query was not created.";
                    good = false;
                }
            }
            else
            {
                // See if name is already used?
                this.sql = "select count(1) from queries where qu_desc = N'$de' and qu_id <> $id";
                this.sql = this.sql.Replace("$de", this.desc.Value.Replace("'", "''"));
                this.sql = this.sql.Replace("$id", Convert.ToString(this.id));
                var query_count = (int) DbUtil.execute_scalar(this.sql);

                if (query_count == 1)
                {
                    this.desc_err.InnerText = "A query with this name already exists.   Choose another name.";
                    this.msg.InnerText = "Query was not created.";
                    good = false;
                }
            }

            return good;
        }

        public void on_update()
        {
            var good = validate();

            if (good)
            {
                if (this.id == 0) // insert new
                {
                    this.sql = @"insert into queries
				(qu_desc, qu_sql, qu_default, qu_user, qu_org)
				values (N'$de', N'$sq', 0, $us, $rl)";
                }
                else // edit existing
                {
                    this.sql = @"update queries set
				qu_desc = N'$de',
				qu_sql = N'$sq',
				qu_user = $us,
				qu_org = $rl
				where qu_id = $id";

                    this.sql = this.sql.Replace("$id", Convert.ToString(this.id));
                }

                this.sql = this.sql.Replace("$de", this.desc.Value.Replace("'", "''"));
                //		if (Util.get_setting("HtmlEncodeSql","0") == "1")
                //		{
                //			sql = sql.Replace("$sq", Server.HtmlDecode(sql_text.Value.Replace("'","''")));
                //		}
                //		else
                //		{
                this.sql = this.sql.Replace("$sq", this.sql_text.Value.Replace("'", "''"));
                //		}

                if (this.security.user.is_admin || this.security.user.can_edit_sql)
                {
                    if (this.vis_everybody.Checked)
                    {
                        this.sql = this.sql.Replace("$us", "0");
                        this.sql = this.sql.Replace("$rl", "0");
                    }
                    else if (this.vis_user.Checked)
                    {
                        this.sql = this.sql.Replace("$us", Convert.ToString(this.user.SelectedItem.Value));
                        this.sql = this.sql.Replace("$rl", "0");
                    }
                    else
                    {
                        this.sql = this.sql.Replace("$rl", Convert.ToString(this.org.SelectedItem.Value));
                        this.sql = this.sql.Replace("$us", "0");
                    }
                }
                else
                {
                    this.sql = this.sql.Replace("$us", Convert.ToString(this.security.user.usid));
                    this.sql = this.sql.Replace("$rl", "0");
                }

                DbUtil.execute_nonquery(this.sql);
                Server.Transfer("queries.aspx");
            }
            else
            {
                if (this.id == 0) // insert new
                    this.msg.InnerText = "Query was not created.";
                else // edit existing
                    this.msg.InnerText = "Query was not updated.";
            }
        }
    }
}
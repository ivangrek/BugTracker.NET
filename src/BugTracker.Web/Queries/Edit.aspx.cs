/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Queries
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using BugTracker.Web.Core.Controls;
    using BugTracker.Web.Core.Persistence;
    using Core;

    public partial class Edit : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public ISecurity Security { get; set; }
        public IQueryService QueryService { get; set; }
        public ApplicationContext ApplicationContext { get; set; }

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.AnyUserOk);

            MainMenu.SelectedItem = MainMenuSections.Queries;

            int.TryParse(Request.QueryString["id"], out var id);

            this.msg.InnerText = string.Empty;

            if (IsPostBack)
            {
                OnUpdate(id);
            }
            else
            {
                Page.Title = $"{ApplicationSettings.AppTitle} - edit query";

                if (IsAuthorized)
                {
                    // these guys can do everything
                    this.visEverybody.Checked = true;

                    // forced project dropdown
                    this.org.DataSource = ApplicationContext.Organisations.ToArray();
                    this.org.DataTextField = "Name";
                    this.org.DataValueField = "Id";
                    this.org.DataBind();
                    this.org.Items.Insert(0, new ListItem("[select org]", "0"));

                    this.user.DataSource = ApplicationContext.Users.ToArray();
                    this.user.DataTextField = "Name";
                    this.user.DataValueField = "Id";
                    this.user.DataBind();
                    this.user.Items.Insert(0, new ListItem("[select user]", "0"));
                }
                else
                {
                    this.sqlText.Visible = false;
                    this.sqlTextLabel.Visible = false;
                    this.explanation.Visible = false;

                    this.visEverybody.Enabled = false;
                    this.visOrg.Enabled = false;
                    this.visUser.Checked = true;
                    this.org.Enabled = false;
                    this.user.Enabled = false;

                    this.org.Visible = false;
                    this.user.Visible = false;
                    this.visEverybody.Visible = false;
                    this.visOrg.Visible = false;
                    this.visUser.Visible = false;
                    this.visibilityLabel.Visible = false;
                }

                // add or edit?
                if (id == 0)
                {
                    this.sub.Value = "Create";
                    this.sqlText.Value = HttpUtility.HtmlDecode(Request.Form["sql_text"]); // if coming from Search.aspx
                }
                else
                {
                    this.sub.Value = "Update";

                    // Get this entry's data from the db and fill in the form
                    var dataRow = QueryService.LoadOne(id);

                    if (dataRow.UserId != Security.User.Usid)
                    {
                        if (!IsAuthorized)
                        {
                            Response.Write("You are not allowed to edit this query");
                            Response.End();
                        }
                    }

                    // Fill in this form
                    this.desc.Value = dataRow.Name;

                    //if (Util.GetSetting("HtmlEncodeSql","0") == "1")
                    //{
                    //  this.sqlText.Value = Server.HtmlEncode(dataRow.Sql);
                    //}
                    //else
                    //{
                    this.sqlText.Value = dataRow.Sql;
                    //}

                    if ((dataRow.UserId == null || dataRow.UserId.Value == 0) && (dataRow.OrganisationId == null || dataRow.OrganisationId.Value == 0))
                    {
                        this.visEverybody.Checked = true;
                    }
                    else if (dataRow.UserId > 0)
                    {
                        this.visUser.Checked = true;

                        foreach (ListItem li in this.user.Items)
                        {
                            if (Convert.ToInt32(li.Value) == dataRow.UserId)
                            {
                                li.Selected = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        this.visOrg.Checked = true;

                        foreach (ListItem li in this.org.Items)
                        {
                            if (Convert.ToInt32(li.Value) == dataRow.OrganisationId)
                            {
                                li.Selected = true;
                                break;
                            }
                        }
                    }
                }
            }
        }

        private bool IsAuthorized => Security.User.IsAdmin
            || Security.User.CanEditSql;

        private void OnUpdate(int id)
        {
            var good = ValidateForm(id);

            if (good)
            {
                var parameters = new Dictionary<string, string>
                {
                    { "$id", Convert.ToString(id)},
                    { "$de", this.desc.Value/*.Replace("'", "''")*/ },
                    { "$sq", string.Empty },
                    { "$us", string.Empty },
                    { "$rl", string.Empty },
                };

                //if (Util.GetSetting("HtmlEncodeSql","0") == "1")
                //{
                //  sql = sql.Replace("$sq", Server.HtmlDecode(sql_text.Value.Replace("'","''")));
                //}
                //else
                //{
                parameters["$sq"] = this.sqlText.Value/*.Replace("'", "''")*/;
                //}

                if (Security.User.IsAdmin || Security.User.CanEditSql)
                {
                    if (this.visEverybody.Checked)
                    {
                        parameters["$us"] = "0";
                        parameters["$rl"] = "0";
                    }
                    else if (this.visUser.Checked)
                    {
                        parameters["$us"] = Convert.ToString(this.user.SelectedItem.Value);
                        parameters["$rl"] = "0";
                    }
                    else
                    {
                        parameters["$us"] = "0";
                        parameters["$rl"] = Convert.ToString(this.org.SelectedItem.Value);
                    }
                }
                else
                {
                    parameters["$us"] = Convert.ToString(Security.User.Usid);
                    parameters["$rl"] = "0";
                }

                if (id == 0) // insert new
                {
                    QueryService.Create(parameters);
                }
                else // edit existing
                {
                    QueryService.Update(parameters);
                }

                Response.Redirect("~/Queries/List.aspx");
            }
            else
            {
                if (id == 0) // insert new
                {
                    this.msg.InnerText = "Query was not created.";
                }
                else // edit existing
                {
                    this.msg.InnerText = "Query was not updated.";
                }
            }
        }

        private bool ValidateForm(int id)
        {
            var good = true;

            if (string.IsNullOrEmpty(this.desc.Value))
            {
                good = false;
                this.descErr.InnerText = "Description is required.";
            }
            else
            {
                this.descErr.InnerText = string.Empty;
            }

            if (Security.User.IsAdmin || Security.User.CanEditSql)
            {
                if (this.visOrg.Checked)
                {
                    if (this.org.SelectedIndex < 1)
                    {
                        good = false;
                        this.orgErr.InnerText = "You must select a org.";
                    }
                    else
                    {
                        this.orgErr.InnerText = "";
                    }
                }
                else if (this.visUser.Checked)
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
                    this.orgErr.InnerText = "";
                }
            }

            if (id == 0)
            {
                // See if name is already used?
                var queryCount = ApplicationContext.Queries
                    .Count(x => x.Name == this.desc.Value.Replace("'", "''"));

                if (queryCount > 0)
                {
                    this.descErr.InnerText = "A query with this name already exists. Choose another name.";
                    this.msg.InnerText = "Query was not created.";
                    good = false;
                }
            }
            else
            {
                // See if name is already used?
                var queryCount = ApplicationContext.Queries
                    .Where(x => x.Name == this.desc.Value.Replace("'", "''"))
                    .Count(x => x.Id != id);

                if (queryCount > 0)
                {
                    this.descErr.InnerText = "A query with this name already exists. Choose another name.";
                    this.msg.InnerText = "Query was not created.";
                    good = false;
                }
            }

            return good;
        }
    }
}
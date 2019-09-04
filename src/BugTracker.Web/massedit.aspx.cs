/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Data;
    using System.IO;
    using System.Text;
    using System.Web.UI;
    using Core;

    public partial class MassEdit : Page
    {
        public string Sql;

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            var security = new Security();

            security.CheckSecurity(Security.AnyUserOkExceptGuest);

            MainMenu.Security = security;
            MainMenu.SelectedItem = "admin";

            if (security.User.IsAdmin || security.User.CanMassEditBugs)
            {
                //
            }
            else
            {
                Response.Write("You are not allowed to use this page.");
                Response.End();
            }

            var list = "";

            if (!IsPostBack)
            {
                Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - "
                                                                            + "massedit";

                if (Request["mass_delete"] != null)
                    this.update_or_delete.Value = "delete";
                else
                    this.update_or_delete.Value = "update";

                // create list of bugs affected
                foreach (string var in Request.QueryString)
                {
                    if (Util.IsInt(var))
                    {
                        if (list != "") list += ",";
                        list += var;
                    }

                    ;
                }

                this.bug_list.Value = list;

                if (this.update_or_delete.Value == "delete")
                {
                    this.update_or_delete.Value = "delete";

                    this.Sql +=
                        "delete bug_post_attachments from bug_post_attachments inner join bug_posts on bug_post_attachments.bpa_post = bug_posts.bp_id where bug_posts.bp_bug in (" +
                        list + ")";
                    this.Sql += "\ndelete from bug_posts where bp_bug in (" + list + ")";
                    this.Sql += "\ndelete from bug_subscriptions where bs_bug in (" + list + ")";
                    this.Sql += "\ndelete from bug_relationships where re_bug1 in (" + list + ")";
                    this.Sql += "\ndelete from bug_relationships where re_bug2 in (" + list + ")";
                    this.Sql += "\ndelete from bug_user where bu_bug in (" + list + ")";
                    this.Sql += "\ndelete from bug_tasks where tsk_bug in (" + list + ")";
                    this.Sql += "\ndelete from bugs where bg_id in (" + list + ")";

                    this.confirm_href.InnerText = "Confirm Delete";
                }
                else
                {
                    this.update_or_delete.Value = "update";

                    this.Sql = "update bugs \nset ";

                    var updates = "";

                    string val;

                    val = Request["mass_project"];
                    if (val != "-1" && Util.IsInt(val))
                    {
                        if (updates != "") updates += ",\n";
                        updates += "bg_project = " + val;
                    }

                    val = Request["mass_org"];
                    if (val != "-1" && Util.IsInt(val))
                    {
                        if (updates != "") updates += ",\n";
                        updates += "bg_org = " + val;
                    }

                    val = Request["mass_category"];
                    if (val != "-1" && Util.IsInt(val))
                    {
                        if (updates != "") updates += ",\n";
                        updates += "bg_category = " + val;
                    }

                    val = Request["mass_priority"];
                    if (val != "-1" && Util.IsInt(val))
                    {
                        if (updates != "") updates += ",\n";
                        updates += "bg_priority = " + val;
                    }

                    val = Request["mass_assigned_to"];
                    if (val != "-1" && Util.IsInt(val))
                    {
                        if (updates != "") updates += ",\n";
                        updates += "bg_assigned_to_user = " + val;
                    }

                    val = Request["mass_reported_by"];
                    if (val != "-1" && Util.IsInt(val))
                    {
                        if (updates != "") updates += ",\n";
                        updates += "bg_reported_user = " + val;
                    }

                    val = Request["mass_status"];
                    if (val != "-1" && Util.IsInt(val))
                    {
                        if (updates != "") updates += ",\n";
                        updates += "bg_status = " + val;
                    }

                    this.Sql += updates + "\nwhere bg_id in (" + list + ")";

                    this.confirm_href.InnerText = "Confirm Update";
                }

                this.sql_text.InnerText = this.Sql;
            }
            else // postback
            {
                list = this.bug_list.Value;

                if (this.update_or_delete.Value == "delete")
                {
                    var uploadFolder = Util.GetUploadFolder();
                    if (uploadFolder != null)
                    {
                        // double check the bug_list
                        var ints = this.bug_list.Value.Split(',');
                        for (var i = 0; i < ints.Length; i++)
                            if (!Util.IsInt(ints[i]))
                                Response.End();

                        var sql2 =
                            @"select bp_bug, bp_id, bp_file from bug_posts where bp_type = 'file' and bp_bug in (" +
                            this.bug_list.Value + ")";
                        var ds = DbUtil.GetDataSet(sql2);
                        foreach (DataRow dr in ds.Tables[0].Rows)
                        {
                            // create path
                            var path = new StringBuilder(uploadFolder);
                            path.Append("\\");
                            path.Append(Convert.ToString(dr["bp_bug"]));
                            path.Append("_");
                            path.Append(Convert.ToString(dr["bp_id"]));
                            path.Append("_");
                            path.Append(Convert.ToString(dr["bp_file"]));
                            if (File.Exists(path.ToString())) File.Delete(path.ToString());
                        }
                    }
                }

                DbUtil.ExecuteNonQuery(this.sql_text.InnerText);
                Response.Redirect("Search.aspx");
            }
        }
    }
}
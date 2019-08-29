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
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class massedit : Page
    {
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

            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK_EXCEPT_GUEST);

            if (this.security.user.is_admin || this.security.user.can_mass_edit_bugs)
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
                Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                            + "massedit";

                if (Request["mass_delete"] != null)
                    this.update_or_delete.Value = "delete";
                else
                    this.update_or_delete.Value = "update";

                // create list of bugs affected
                foreach (string var in Request.QueryString)
                {
                    if (Util.is_int(var))
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

                    this.sql +=
                        "delete bug_post_attachments from bug_post_attachments inner join bug_posts on bug_post_attachments.bpa_post = bug_posts.bp_id where bug_posts.bp_bug in (" +
                        list + ")";
                    this.sql += "\ndelete from bug_posts where bp_bug in (" + list + ")";
                    this.sql += "\ndelete from bug_subscriptions where bs_bug in (" + list + ")";
                    this.sql += "\ndelete from bug_relationships where re_bug1 in (" + list + ")";
                    this.sql += "\ndelete from bug_relationships where re_bug2 in (" + list + ")";
                    this.sql += "\ndelete from bug_user where bu_bug in (" + list + ")";
                    this.sql += "\ndelete from bug_tasks where tsk_bug in (" + list + ")";
                    this.sql += "\ndelete from bugs where bg_id in (" + list + ")";

                    this.confirm_href.InnerText = "Confirm Delete";
                }
                else
                {
                    this.update_or_delete.Value = "update";

                    this.sql = "update bugs \nset ";

                    var updates = "";

                    string val;

                    val = Request["mass_project"];
                    if (val != "-1" && Util.is_int(val))
                    {
                        if (updates != "") updates += ",\n";
                        updates += "bg_project = " + val;
                    }

                    val = Request["mass_org"];
                    if (val != "-1" && Util.is_int(val))
                    {
                        if (updates != "") updates += ",\n";
                        updates += "bg_org = " + val;
                    }

                    val = Request["mass_category"];
                    if (val != "-1" && Util.is_int(val))
                    {
                        if (updates != "") updates += ",\n";
                        updates += "bg_category = " + val;
                    }

                    val = Request["mass_priority"];
                    if (val != "-1" && Util.is_int(val))
                    {
                        if (updates != "") updates += ",\n";
                        updates += "bg_priority = " + val;
                    }

                    val = Request["mass_assigned_to"];
                    if (val != "-1" && Util.is_int(val))
                    {
                        if (updates != "") updates += ",\n";
                        updates += "bg_assigned_to_user = " + val;
                    }

                    val = Request["mass_reported_by"];
                    if (val != "-1" && Util.is_int(val))
                    {
                        if (updates != "") updates += ",\n";
                        updates += "bg_reported_user = " + val;
                    }

                    val = Request["mass_status"];
                    if (val != "-1" && Util.is_int(val))
                    {
                        if (updates != "") updates += ",\n";
                        updates += "bg_status = " + val;
                    }

                    this.sql += updates + "\nwhere bg_id in (" + list + ")";

                    this.confirm_href.InnerText = "Confirm Update";
                }

                this.sql_text.InnerText = this.sql;
            }
            else // postback
            {
                list = this.bug_list.Value;

                if (this.update_or_delete.Value == "delete")
                {
                    var upload_folder = Util.get_upload_folder();
                    if (upload_folder != null)
                    {
                        // double check the bug_list
                        var ints = this.bug_list.Value.Split(',');
                        for (var i = 0; i < ints.Length; i++)
                            if (!Util.is_int(ints[i]))
                                Response.End();

                        var sql2 =
                            @"select bp_bug, bp_id, bp_file from bug_posts where bp_type = 'file' and bp_bug in (" +
                            this.bug_list.Value + ")";
                        var ds = DbUtil.get_dataset(sql2);
                        foreach (DataRow dr in ds.Tables[0].Rows)
                        {
                            // create path
                            var path = new StringBuilder(upload_folder);
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

                DbUtil.execute_nonquery(this.sql_text.InnerText);
                Response.Redirect("search.aspx");
            }
        }
    }
}
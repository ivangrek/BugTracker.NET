/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Web;
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using Core;

    public partial class bugs : Page
    {
        public DataSet ds_custom_cols = null;
        public DataView dv;
        public string qu_id_string;
        public Security security;

        public string sql;
        public string sql_error = "";

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                        + Util.get_setting("PluralBugLabel",
                                                                            "bugs");

            if (!IsPostBack)
            {
                load_query_dropdown();

                if (Session["just_did_text_search"] == null)
                {
                    do_query();
                }
                else
                {
                    Session["just_did_text_search"] = null;
                    this.dv = (DataView) Session["bugs"];
                }
            }
            else
            {
                // posting back a query change?
                // posting back a filter change?
                // posting back a sort change?

                if (this.actn.Value == "query")
                {
                    this.qu_id_string = Convert.ToString(this.query.SelectedItem.Value);
                    reset_query_state();
                    do_query();
                }
                else
                {
                    // sorting, paging, filtering, so don't go back to the database

                    this.dv = (DataView) Session["bugs"];
                    if (this.dv == null)
                    {
                        do_query();
                    }
                    else
                    {
                        if (this.actn.Value == "sort") this.new_page.Value = "0";
                    }
                }
            }

            select_query_in_dropdown();

            call_sort_and_filter_buglist_dataview();

            this.actn.Value = "";
        }

        public void select_query_in_dropdown()
        {
            // select drop down based on whatever query we ended up using
            if (this.qu_id_string != null)
                foreach (ListItem li in this.query.Items)
                    if (li.Value == this.qu_id_string)
                    {
                        li.Selected = true;
                        break;
                    }
        }

        public void reset_query_state()
        {
            this.new_page.Value = "0";
            this.filter.Value = "";
            this.sort.Value = "-1";
            this.prev_sort.Value = "-1";
            this.prev_dir.Value = "ASC";
        }

        public void do_query()
        {
            // figure out what SQL to run and run it.

            string bug_sql = null;

            // From the URL
            if (this.qu_id_string == null)
                // specified in URL?
                this.qu_id_string = Util.sanitize_integer(Request["qu_id"]);

            // From a previous viewing of this page
            if (this.qu_id_string == null)
                // Is there a previously selected query, from a use of this page
                // earlier in this session?
                this.qu_id_string = (string) Session["SelectedBugQuery"];

            if (this.qu_id_string != null && this.qu_id_string != "" && this.qu_id_string != "0")
            {
                // Use sql specified in query string.
                // This is the normal path from the queries page.
                this.sql = @"select qu_sql from queries where qu_id = $quid";
                this.sql = this.sql.Replace("$quid", this.qu_id_string);
                bug_sql = (string) DbUtil.execute_scalar(this.sql);
            }

            if (bug_sql == null)
            {
                // This is the normal path after logging in.
                // Use sql associated with user
                this.sql = @"select qu_id, qu_sql from queries where qu_id in
			(select us_default_query from users where us_id = $us)";
                this.sql = this.sql.Replace("$us", Convert.ToString(this.security.user.usid));
                var dr = DbUtil.get_datarow(this.sql);
                if (dr != null)
                {
                    this.qu_id_string = Convert.ToString(dr["qu_id"]);
                    bug_sql = (string) dr["qu_sql"];
                }
            }

            // As a last resort, grab some query.
            if (bug_sql == null)
            {
                this.sql =
                    @"select top 1 qu_id, qu_sql from queries order by case when qu_default = 1 then 1 else 0 end desc";
                var dr = DbUtil.get_datarow(this.sql);
                bug_sql = (string) dr["qu_sql"];
                if (dr != null)
                {
                    this.qu_id_string = Convert.ToString(dr["qu_id"]);
                    bug_sql = (string) dr["qu_sql"];
                }
            }

            if (bug_sql == null)
            {
                Response.Write(
                    "Error!. No queries available for you to use!<p>Please contact your BugTracker.NET administrator.");
                Response.End();
            }

            // Whatever query we used, select it in the drop down
            if (this.qu_id_string != null)
            {
                foreach (ListItem li in this.query.Items) li.Selected = false;
                foreach (ListItem li in this.query.Items)
                    if (li.Value == this.qu_id_string)
                    {
                        li.Selected = true;
                        break;
                    }
            }

            // replace magic variables
            bug_sql = bug_sql.Replace("$ME", Convert.ToString(this.security.user.usid));

            bug_sql = Util.alter_sql_per_project_permissions(bug_sql, this.security);

            if (Util.get_setting("UseFullNames", "0") == "0")
                // false condition
                bug_sql = bug_sql.Replace("$fullnames", "0 = 1");
            else
                // true condition
                bug_sql = bug_sql.Replace("$fullnames", "1 = 1");

            // run the query
            DataSet ds = null;
            try
            {
                ds = DbUtil.get_dataset(bug_sql);
                this.dv = new DataView(ds.Tables[0]);
            }
            catch (SqlException e)
            {
                this.sql_error = e.Message;
                this.dv = null;
            }

            // Save it.
            Session["bugs"] = this.dv;
            Session["SelectedBugQuery"] = this.qu_id_string;

            // Save it again.  We use the unfiltered query to determine the
            // values that go in the filter dropdowns.
            if (ds != null)
                Session["bugs_unfiltered"] = ds.Tables[0];
            else
                Session["bugs_unfiltered"] = null;
        }

        public void load_query_dropdown()
        {
            // populate query drop down
            this.sql = @"/* query dropdown */
select qu_id, qu_desc
from queries
where (isnull(qu_user,0) = 0 and isnull(qu_org,0) = 0)
or isnull(qu_user,0) = $us
or isnull(qu_org,0) = $org
order by qu_desc";

            this.sql = this.sql.Replace("$us", Convert.ToString(this.security.user.usid));
            this.sql = this.sql.Replace("$org", Convert.ToString(this.security.user.org));

            this.query.DataSource = DbUtil.get_dataview(this.sql);

            this.query.DataTextField = "qu_desc";
            this.query.DataValueField = "qu_id";
            this.query.DataBind();
        }

        public void display_bugs(bool show_checkboxes)
        {
            BugList.display_bugs(
                show_checkboxes, this.dv,
                Response, this.security, this.new_page.Value,
                IsPostBack, this.ds_custom_cols, this.filter.Value);
        }

        public void call_sort_and_filter_buglist_dataview()
        {
            var filter_val = this.filter.Value;
            var sort_val = this.sort.Value;
            var prev_sort_val = this.prev_sort.Value;
            var prev_dir_val = this.prev_dir.Value;

            BugList.sort_and_filter_buglist_dataview(this.dv, IsPostBack, this.actn.Value,
                ref filter_val,
                ref sort_val,
                ref prev_sort_val,
                ref prev_dir_val);

            this.filter.Value = filter_val;
            this.sort.Value = sort_val;
            this.prev_sort.Value = prev_sort_val;
            this.prev_dir.Value = prev_dir_val;
        }
    }
}
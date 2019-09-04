/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Bugs
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using Core;

    public partial class List : Page
    {
        public DataSet DsCustomCols = null;
        public DataView Dv;
        public string QuIdString;

        public string Sql;
        public string SqlError = string.Empty;

        public Security Security { get; set; }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            var security = new Security();

            security.CheckSecurity(Security.AnyUserOk);

            Security = security;

            MainMenu.Security = security;
            MainMenu.SelectedItem = Util.GetSetting("PluralBugLabel", "bugs");

            Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - " + Util.GetSetting("PluralBugLabel", "bugs");

            if (!IsPostBack)
            {
                load_query_dropdown(security);

                if (Session["just_did_text_search"] == null)
                {
                    do_query(security);
                }
                else
                {
                    Session["just_did_text_search"] = null;
                    this.Dv = (DataView) Session["bugs"];
                }
            }
            else
            {
                // posting back a query change?
                // posting back a filter change?
                // posting back a sort change?

                if (this.actn.Value == "query")
                {
                    this.QuIdString = Convert.ToString(this.query.SelectedItem.Value);
                    reset_query_state();
                    do_query(security);
                }
                else
                {
                    // sorting, paging, filtering, so don't go back to the database

                    this.Dv = (DataView) Session["bugs"];
                    if (this.Dv == null)
                    {
                        do_query(security);
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
            if (this.QuIdString != null)
                foreach (ListItem li in this.query.Items)
                    if (li.Value == this.QuIdString)
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

        public void do_query(Security security)
        {
            // figure out what SQL to run and run it.

            string bugSql = null;

            // From the URL
            if (this.QuIdString == null)
                // specified in URL?
                this.QuIdString = Util.SanitizeInteger(Request["qu_id"]);

            // From a previous viewing of this page
            if (this.QuIdString == null)
                // Is there a previously selected query, from a use of this page
                // earlier in this session?
                this.QuIdString = (string) Session["SelectedBugQuery"];

            if (this.QuIdString != null && this.QuIdString != "" && this.QuIdString != "0")
            {
                // Use sql specified in query string.
                // This is the normal path from the queries page.
                this.Sql = @"select qu_sql from queries where qu_id = $quid";
                this.Sql = this.Sql.Replace("$quid", this.QuIdString);
                bugSql = (string) DbUtil.ExecuteScalar(this.Sql);
            }

            if (bugSql == null)
            {
                // This is the normal path after logging in.
                // Use sql associated with user
                this.Sql = @"select qu_id, qu_sql from queries where qu_id in
            (select us_default_query from users where us_id = $us)";
                this.Sql = this.Sql.Replace("$us", Convert.ToString(security.User.Usid));
                var dr = DbUtil.GetDataRow(this.Sql);
                if (dr != null)
                {
                    this.QuIdString = Convert.ToString(dr["qu_id"]);
                    bugSql = (string) dr["qu_sql"];
                }
            }

            // As a last resort, grab some query.
            if (bugSql == null)
            {
                this.Sql =
                    @"select top 1 qu_id, qu_sql from queries order by case when qu_default = 1 then 1 else 0 end desc";
                var dr = DbUtil.GetDataRow(this.Sql);
                bugSql = (string) dr["qu_sql"];
                if (dr != null)
                {
                    this.QuIdString = Convert.ToString(dr["qu_id"]);
                    bugSql = (string) dr["qu_sql"];
                }
            }

            if (bugSql == null)
            {
                Response.Write(
                    "Error!. No queries available for you to use!<p>Please contact your BugTracker.NET administrator.");
                Response.End();
            }

            // Whatever query we used, select it in the drop down
            if (this.QuIdString != null)
            {
                foreach (ListItem li in this.query.Items) li.Selected = false;
                foreach (ListItem li in this.query.Items)
                    if (li.Value == this.QuIdString)
                    {
                        li.Selected = true;
                        break;
                    }
            }

            // replace magic variables
            bugSql = bugSql.Replace("$ME", Convert.ToString(security.User.Usid));

            bugSql = Util.AlterSqlPerProjectPermissions(bugSql, security);

            if (Util.GetSetting("UseFullNames", "0") == "0")
                // false condition
                bugSql = bugSql.Replace("$fullnames", "0 = 1");
            else
                // true condition
                bugSql = bugSql.Replace("$fullnames", "1 = 1");

            // run the query
            DataSet ds = null;
            try
            {
                ds = DbUtil.GetDataSet(bugSql);
                this.Dv = new DataView(ds.Tables[0]);
            }
            catch (SqlException e)
            {
                this.SqlError = e.Message;
                this.Dv = null;
            }

            // Save it.
            Session["bugs"] = this.Dv;
            Session["SelectedBugQuery"] = this.QuIdString;

            // Save it again.  We use the unfiltered query to determine the
            // values that go in the filter dropdowns.
            if (ds != null)
                Session["bugs_unfiltered"] = ds.Tables[0];
            else
                Session["bugs_unfiltered"] = null;
        }

        public void load_query_dropdown(Security security)
        {
            // populate query drop down
            this.Sql = @"/* query dropdown */
select qu_id, qu_desc
from queries
where (isnull(qu_user,0) = 0 and isnull(qu_org,0) = 0)
or isnull(qu_user,0) = $us
or isnull(qu_org,0) = $org
order by qu_desc";

            this.Sql = this.Sql.Replace("$us", Convert.ToString(security.User.Usid));
            this.Sql = this.Sql.Replace("$org", Convert.ToString(security.User.Org));

            this.query.DataSource = DbUtil.GetDataView(this.Sql);

            this.query.DataTextField = "qu_desc";
            this.query.DataValueField = "qu_id";
            this.query.DataBind();
        }

        public void display_bugs(bool showCheckboxes, Security security)
        {
            BugList.DisplayBugs(
                showCheckboxes, this.Dv,
                Response, security, this.new_page.Value,
                IsPostBack, this.DsCustomCols, this.filter.Value);
        }

        public void call_sort_and_filter_buglist_dataview()
        {
            var filterVal = this.filter.Value;
            var sortVal = this.sort.Value;
            var prevSortVal = this.prev_sort.Value;
            var prevDirVal = this.prev_dir.Value;

            BugList.SortAndFilterBugListDataView(this.Dv, IsPostBack, this.actn.Value,
                ref filterVal,
                ref sortVal,
                ref prevSortVal,
                ref prevDirVal);

            this.filter.Value = filterVal;
            this.sort.Value = sortVal;
            this.prev_sort.Value = prevSortVal;
            this.prev_dir.Value = prevDirVal;
        }
    }
}
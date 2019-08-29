/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Data;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class query : Page
    {
        public DataSet ds;

        public string exception_message;
        public Security security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            // If there is a users table, then authenticate this page
            try
            {
                DbUtil.execute_nonquery("select count(1) from users");
                this.security = new Security();
                this.security.check_security(HttpContext.Current, Security.MUST_BE_ADMIN);
            }
            catch (Exception)
            {
            }

            Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "run query";

            if (IsPostBack)
            {
                if (this.queryText.Value != "")
                    try
                    {
                        this.ds = DbUtil.get_dataset(Server.HtmlDecode(this.queryText.Value));
                    }
                    catch (Exception e2)
                    {
                        this.exception_message = e2.Message;
                        //exception_message = e2.ToString();  // uncomment this if you need more error info.
                    }
            }
            else
            {
                var ds = DbUtil.get_dataset("select name from sysobjects where type = 'u' order by 1");
                this.dbtables_select.Items.Add("Select Table");
                foreach (DataRow dr in ds.Tables[0].Rows) this.dbtables_select.Items.Add((string) dr[0]);
            }
        }
    }
}
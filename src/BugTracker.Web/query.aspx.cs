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

    public partial class Query : Page
    {
        public DataSet Ds;

        public string ExceptionMessage;
        public Security Security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            // If there is a users table, then authenticate this page
            try
            {
                DbUtil.ExecuteNonQuery("select count(1) from users");
                this.Security = new Security();
                this.Security.CheckSecurity(HttpContext.Current, Security.MustBeAdmin);
            }
            catch (Exception)
            {
            }

            Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "run query";

            if (IsPostBack)
            {
                if (this.queryText.Value != "")
                    try
                    {
                        this.Ds = DbUtil.GetDataSet(Server.HtmlDecode(this.queryText.Value));
                    }
                    catch (Exception e2)
                    {
                        this.ExceptionMessage = e2.Message;
                        //exception_message = e2.ToString();  // uncomment this if you need more error info.
                    }
            }
            else
            {
                var ds = DbUtil.GetDataSet("select name from sysobjects where type = 'u' order by 1");
                this.dbtables_select.Items.Add("Select Table");
                foreach (DataRow dr in ds.Tables[0].Rows) this.dbtables_select.Items.Add((string) dr[0]);
            }
        }
    }
}
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
    using Core;

    public partial class GenerateBtnetscReg : Page
    {
        public Security Security;

        public void Page_Load(object sender, EventArgs e)
        {
            this.Security = new Security();
            this.Security.CheckSecurity(HttpContext.Current, Security.AnyUserOk);

            Response.ContentType = "text/reg";
            Response.AddHeader("content-disposition", "attachment; filename=\"btnetsc.reg\"");
            Response.Write("Windows Registry Editor Version 5.00");
            Response.Write("\n\n");
            Response.Write("[HKEY_CURRENT_USER\\Software\\BugTracker.NET\\btnetsc\\SETTINGS]" + "\n");

            var url = "http://" + Request.ServerVariables["SERVER_NAME"] + Request.ServerVariables["URL"];
            url = url.Replace("generate_btnetsc_reg", "insert_bug");
            write_variable_value("Url", url);
            write_variable_value("Project", "0");
            write_variable_value("Email", this.Security.User.Email);
            write_variable_value("Username", this.Security.User.Username);

            var nvcSrvElements = Request.ServerVariables;
            var array1 = nvcSrvElements.AllKeys;
        }

        public void write_variable_value(string var, string val)
        {
            Response.Write("\"" + var + "\"=\"" + val + "\"\n");
        }
    }
}
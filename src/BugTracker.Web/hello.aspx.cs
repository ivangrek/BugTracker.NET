/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Globalization;
    using System.Threading;
    using System.Web.UI;

    public partial class Hello : Page
    {
        public void Page_Load(object sender, EventArgs e)
        {
            Response.Write("Hello<br>");

            Response.Write(Thread.CurrentThread.CurrentCulture.Name);

            Response.Write("<br>");

            var ci =
                new CultureInfo(Thread.CurrentThread.CurrentCulture.Name);

            Response.Write(ci.NumberFormat.NumberDecimalSeparator);

            ci =
                new CultureInfo("de-DE");

            Response.Write(ci.NumberFormat.NumberDecimalSeparator);
        }
    }
}
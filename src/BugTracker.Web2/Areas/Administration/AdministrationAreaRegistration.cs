/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Areas.Administration
{
    using System.Web.Mvc;

    public class AdministrationAreaRegistration : AreaRegistration
    {
        public override string AreaName => "Administration";

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                "Administration_Default",
                "Administration/{controller}/{action}/{id}",
                new
                {
                    controller = "Home",
                    action = "Index",
                    id = UrlParameter.Optional
                }
            );
        }
    }
}
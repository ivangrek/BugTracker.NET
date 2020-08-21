/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Areas.Versioning
{
    using System.Web.Mvc;

    public class VersioningAreaRegistration : AreaRegistration
    {
        public override string AreaName => "Versioning";

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                "Versioning_Default",
                "Versioning/{controller}/{action}/{id}",
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
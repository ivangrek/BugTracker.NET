/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System.Web.Mvc;
    using System.Web.Routing;

    internal static class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("{resource}.aspx/{*pathInfo}");

            routes.MapRoute(
                "Default",
                string.Empty,
                new
                {
                    controller = "Account",
                    action = "Login"
                }
            );

            routes.MapRoute(
                "Main",
                "{controller}/{action}/{id}",
                new
                {
                    action = "Index",
                    id = UrlParameter.Optional
                }
            );
        }
    }
}
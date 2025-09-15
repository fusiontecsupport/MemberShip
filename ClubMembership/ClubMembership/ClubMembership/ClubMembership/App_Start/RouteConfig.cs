using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace ClubMembership
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            //defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }

            // Enable attribute routing
            routes.MapMvcAttributeRoutes();


            // Explicit route for CategoryMaster under Masters namespace
            routes.MapRoute(
                name: "CategoryMaster",
                url: "CategoryMaster/{action}/{id}",
                defaults: new { controller = "CategoryMaster", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "ClubMembership.Controllers.Masters" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Account", action = "Login", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Members",
                url: "Members/{action}/{id}",
                defaults: new { controller = "Members", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}

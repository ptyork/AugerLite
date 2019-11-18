using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Auger
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapMvcAttributeRoutes();

            // don't forget to add webserver handlers for these in Web.config
            routes.MapRoute(
                name: "BrowseSelf",
                url: "Browse/{courseId}/{assignmentId}/{*pathInfo}",
                defaults: new { controller = "Browse", action = "BrowseSelf" }
            );

            routes.MapRoute(
                name: "BrowseUser",
                url: "BrowseUser/{courseId}/{userName}/{assignmentId}/{*pathInfo}",
                defaults: new { controller = "Browse", action = "BrowseUser" }
            );

            routes.MapRoute(
                name: "BrowseWork",
                url: "BrowseWork/{courseId}/{assignmentId}/{*pathInfo}",
                defaults: new { controller = "Browse", action = "BrowseWork" }
            );

            routes.MapRoute(
                name: "BrowseUserWork",
                url: "BrowseUserWork/{courseId}/{userName}/{assignmentId}/{*pathInfo}",
                defaults: new { controller = "Browse", action = "BrowseUserWork" }
            );

            routes.MapRoute(
                name: "BrowsePlay",
                url: "BrowsePlay/{courseId}/{userName}/{playgroundId}/{*pathInfo}",
                defaults: new { controller = "Browse", action = "BrowsePlay" }
            );

            routes.MapRoute(
                name: "BrowseAny",
                url: "BrowseAny",
                defaults: new { controller = "Browse", action = "BrowseAny" }
            );

            routes.MapRoute(
                name: "CourseAdmin",
                url: "CourseAdmin/{action}/{courseId}/{id}/{selectedId}",
                defaults: new {
                    controller = "CourseAdmin",
                    action = "Index",
                    courseId = UrlParameter.Optional,
                    id = UrlParameter.Optional,
                    selectedId = UrlParameter.Optional
                }
            );

            routes.MapRoute(
                name: "Grader",
                url: "Grade/{action}/{*pathInfo}",
                defaults: new { controller = "Grade", action = "Index", pathInfo = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}/{secondaryId}",
                defaults: new {
                    controller = "Home",
                    action = "Index",
                    id = UrlParameter.Optional,
                    secondaryId = UrlParameter.Optional
                }
            );

        }
    }
}

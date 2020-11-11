using Auger;
using Auger.DAL;
using Auger.Models;
using Auger.Models.Data;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Auger
{
    public class CourseAuthorizeAttribute : AuthorizeAttribute
    {
        private readonly string[] _allowedRoles;

        public CourseAuthorizeAttribute(params string[] roles)
        {
            _allowedRoles = roles;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            filterContext.Result = new RedirectToRouteResult(
            new RouteValueDictionary
            {
                { "action", "Unauthorized" },
                { "controller", "Home" }
            });
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var user = ApplicationUser.Current;

            if (user == null)
            {
                return false;
            }

            // Allow SuperUser no matter what
            if (user.Roles.Where(r => r.RoleId.ToLowerInvariant() == UserRoles.SuperUserRole.ToLowerInvariant()).Any())
            {
                return true;
            }

            int courseId = CookieManager.GetCourseId();
            if (courseId == 0)
            {
                // TODO: WARNING...this returns true when no course is selected
                return true;
            }

            using (var db = new AugerContext())
            {
                var enrollment = db.Enrollments.Where(e => e.CourseId == courseId && e.UserId == user.Id).FirstOrDefault();

                if (enrollment == null)
                {
                    return false;
                }

                if (_allowedRoles.Length == 0)
                {
                    return true;
                }

                foreach (var role in _allowedRoles)
                {
                    if (enrollment.IsInRole(role))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}

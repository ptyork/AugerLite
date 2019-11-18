using Auger.DAL;
using Auger.Models;
using Auger.Models.Data;
using System;
using System.Linq;
using System.Web.Mvc;

namespace Auger.Controllers
{
    public abstract class AugerControllerBase : Controller
    {
        protected AugerContext _db = new AugerContext();

        protected Course _GetCourse(int courseId = 0)
        {
            Course course = null;
            try
            {
                if (courseId == 0)
                {
                    courseId = CookieManager.GetCourseId();
                }
                if (User.IsInRole(UserRoles.SuperUserRole))
                {
                    // a super user can see any course regardless of enrollment
                    course = _db.Courses.FirstOrDefault(c => c.CourseId == courseId);
                }
                else
                {
                    var userId = ApplicationUser.Current.Id;
                    if (courseId > 0)
                    {
                        // get the currently selected course
                        course = _db.Enrollments
                            .Where(e => e.CourseId == courseId)
                            .Where(e => e.UserId == userId)
                            .Select(e => e.Course)
                            .FirstOrDefault();
                    }
                    else
                    {
                        // if there's only one course for the user, return that course
                        var courses = _db.Enrollments
                            .Where(e => e.UserId == userId)
                            .Select(e => e.Course);
                        if (courses.Count() == 1)
                        {
                            course = courses.First();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
            }
            return course;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
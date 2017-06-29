using Auger;
using Auger.DAL;
using Auger.Models;
using Auger.Models.Data;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Auger.Controllers
{
    public class PreSubmissionController : Controller
    {
        private AugerContext db = new AugerContext();

        private Course _GetCourse()
        {
            var courseId = CookieManager.GetCourseId();
            Course course = db.Courses
                .Include(c => c.Assignments)
                .Include(c => c.Assignments.Select(a => a.Pages))
                .FirstOrDefault(c => c.CourseId == courseId);
            // TODO: Retrieve course if only one for user
            return course;
        }

        public JsonpResult TestLogin()
        {
            return new JsonpResult(!string.IsNullOrWhiteSpace(User.GetName()));
        }

        [Authorize]
        public JsonpResult GetAllAssignments()
        {
            Course course = _GetCourse();
            if (course == null)
            {
                return new JsonpResult();
            }
            return new JsonpResult(course.Assignments);
        }

        [Authorize]
        public JsonpResult GetAllCourses()
        {
            var user = ApplicationUser.Current;
            var courses = db.Enrollments.Where(e => e.UserId == user.Id).Select(e => e.Course);
            return new JsonpResult(courses);
        }

        public ContentResult GetScript(int assignmentId, string pageName, int viewportWidth)
        {
            var assignment = db.Assignments.FirstOrDefault(a => a.AssignmentId == assignmentId);
            var page = db.Pages.FirstOrDefault(p => p.AssignmentId == assignmentId && p.PageName.Equals(pageName, StringComparison.InvariantCultureIgnoreCase));
            var script = SubmissionTester.GetPreTestScript(assignment, page, viewportWidth);
            return Content(script, "application/javascript");
        }

        public class PostFile
        {
            public string FileName { get; set; }
            [AllowHtml]
            public string Text { get; set; }
        }

        [HttpPost]
        public JsonResult ValidateHTML(PostFile file)
        {
            return Json(W3CValidator.ValidateHTML(file.FileName, file.Text));
        }

        [HttpPost]
        public JsonResult ValidateCSS(PostFile file)
        {
            return Json(W3CValidator.ValidateCSS(file.FileName, file.Text));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
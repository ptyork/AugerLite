using Auger;
using Auger.DAL;
using Auger.Models;
using Auger.Models.Data;
using Auger.Models.View;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Auger.Controllers
{
    [Authorize]
    public class AssignmentController : Controller
    {
        private AugerContext db = new AugerContext();

        private Course _GetCourse()
        {
            var courseId = CookieManager.GetCourseId();
            Course course = db.Courses.FirstOrDefault(c => c.CourseId == courseId);
            // TODO: Retrieve course if only one for user
            return course;
        }

        public ActionResult Index()
        {
            Course course = _GetCourse();
            if (course == null)
            {
                return RedirectToAction("SelectCourse");
            }

            AssignmentIndexViewModel vm = new AssignmentIndexViewModel();
            vm.Course = course;

            var user = ApplicationUser.Current;

            var studentAssignments = db.StudentAssignments
                .Include(sa => sa.Submissions)
                .Include(sa => sa.Assignment)
                .Where(sa => sa.Enrollment.UserName == user.UserName && sa.Assignment.CourseId == course.CourseId)
                .ToList();

            foreach (var sa in studentAssignments)
            {
                if (sa.Submissions.Any())
                {
                    vm.SubmittedAssignments.Add(sa.Submissions.LastOrDefault());
                }
                else
                {
                    vm.UnsubmittedAssignments.Add(sa.Assignment);
                }
            }

            return View(vm);
        }

        public ActionResult ClearCourse()
        {
            CookieManager.ClearCourseId();
            return RedirectToAction("Index");
        }

        public ActionResult SelectCourse(int id = 0)
        {
            if (id == 0)
            {
                var userName = User.GetName();
                var courses = db.Enrollments
                    .Where(e => e.UserName == userName)
                    .Select(e => e.Course)
                    .AsEnumerable();
                var model = new CourseSelectViewModel
                {
                    Courses = courses
                };
                return View(model);
            }

            CookieManager.SetCourseId(id);
            return RedirectToAction("Index");
        }

        public ActionResult Details(int id)
        {
            return SubmissionDetails(id, -1);
        }

        public ActionResult SubmissionDetails(int id, int submissionId)
        {
            Course course = _GetCourse();
            if (course == null)
            {
                return RedirectToAction("SelectCourse");
            }

            Assignment assignment = course.Assignments.FirstOrDefault(a => a.AssignmentId == id);
            if (assignment == null)
            {
                return new HttpNotFoundResult();
            }

            AssignmentDetailsViewModel vm = new AssignmentDetailsViewModel();
            vm.Assignment = assignment;

            var user = ApplicationUser.Current;

            var studentAssignment = db.StudentAssignments
                .Include(sa => sa.Submissions)
                .Include(sa => sa.Assignment)
                .Include(sa => sa.Enrollment.User)
                .FirstOrDefault(sa => sa.AssignmentId == assignment.AssignmentId && sa.Enrollment.UserName == user.UserName);

            vm.Submissions = studentAssignment.Submissions.OrderBy(s => s.DateCreated).ToList();

            var selectedSubmission = vm.Submissions.LastOrDefault();

            if (selectedSubmission != null && submissionId > 0)
            {
                selectedSubmission = vm.Submissions.FirstOrDefault(s => s.StudentSubmissionId == submissionId);
                if (selectedSubmission == null)
                {
                    return new HttpNotFoundResult();
                }
            }

            if (selectedSubmission != null)
            {
                var repo = SubmissionRepository.Get(studentAssignment);
                repo.CheckoutSubmission(selectedSubmission.CommitId);
                var work = TempDir.Get(repo);
                vm.SelectedSubmission = new SubmissionViewModel()
                {
                    Submission = selectedSubmission,
                    Folder = SubmissionManager.GetFolder(selectedSubmission)
                };
                repo.CheckoutSubmission();
            }

            return View("Details", vm);
        }

        [HttpPost]
        public ActionResult Submit(SubmissionModel model, HttpPostedFileBase zipFile)
        {
            model.ZipFile = zipFile;
            var results = SubmissionManager.Submit(model);
            TempData["results"] = results;
            return RedirectToAction("ViewAssignment", new { id = model.AssignmentId });
        }

        [Authorize]
        public JsonResult SubmitAjax(SubmissionModel model)
        {
            var results = SubmissionManager.Submit(model);
            return Json(results);
        }

        public class TestModel
        {
            public int CourseId { get; set; }
            public int AssignmentId { get; set; }
            public int SubmissionId { get; set; }
        }

        [HttpPost]
        public JsonResult TestAjax(TestModel model)
        {
            var results = _Test(model.CourseId, model.AssignmentId, model.SubmissionId);
            return Json(results);
        }

        private TestResults _Test(int courseId, int assignmentId, int submissionId)
        {
            var user = ApplicationUser.Current;

            var assignment = db.Assignments.FirstOrDefault(a => a.CourseId == courseId && a.AssignmentId == assignmentId);

            var studentAssignment = db.StudentAssignments
                .Include(sa => sa.Submissions)
                .Include(sa => sa.Assignment.Pages)
                .Include(sa => sa.Enrollment.User)
                .FirstOrDefault(sa => sa.AssignmentId == assignmentId && sa.Enrollment.UserName == user.UserName);

            if (studentAssignment == null)
            {
                return null;
            }

            StudentSubmission submission;
            if (submissionId > 0)
            {
                submission = studentAssignment.Submissions.FirstOrDefault(s => s.StudentSubmissionId == submissionId);
            }
            else
            {
                submission = studentAssignment.Submissions.LastOrDefault();
            }

            if (submission == null)
            {
                return null;
            }

            SubmissionTester.TestSubmission(submission);
            //db.Entry(submission).State = EntityState.Modified;
            db.SaveChanges();

            return submission.PreSubmissionResults;
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
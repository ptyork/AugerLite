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
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace Auger.Controllers
{
    [Authorize]
    public class AssignmentController : IDEControllerBase<WorkRepository>
    {
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

            var studentAssignments = _db.StudentAssignments
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
                var courses = _db.Enrollments
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

            ApplicationUser user = ApplicationUser.Current;

            AssignmentDetailsViewModel vm = new AssignmentDetailsViewModel();
            vm.User = user;
            vm.Course = course;
            vm.Assignment = assignment;

            vm.Submissions = _db.StudentSubmissions
                .Where(s => s.StudentAssignment.AssignmentId == id)
                .Where(s => s.StudentAssignment.Enrollment.UserId == user.Id)
                .OrderBy(s => s.DateCreated)
                .ToList();

            return View(vm);
        }

        public ActionResult Edit(int id)
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

            var user = ApplicationUser.Current;

            var studentAssignment = _db.StudentAssignments
                .Include(sa => sa.Submissions)
                .FirstOrDefault(sa => sa.AssignmentId == assignment.AssignmentId && sa.Enrollment.UserName == user.UserName);
            if (studentAssignment == null)
            {
                return new HttpNotFoundResult();
            }

            AssignmentEditViewModel vm = new AssignmentEditViewModel()
            {
                User = user,
                Assignment = assignment,
                Course = course,
                Submissions = studentAssignment.Submissions.OrderBy(s => s.DateCreated).ToList()
            };
            return View(vm);
        }

        [HttpPost]
        public ActionResult GetSubmissionDetails(SubmissionPostData data)
        {
            Course course = _GetCourse();
            if (course == null || course.CourseId != data.CourseId)
            {
                return new HttpNotFoundResult();
            }

            var user = ApplicationUser.Current;
            if (user == null || user.Id != data.UserId)
            {
                return new HttpNotFoundResult();
            }

            try
            {
                var submission = _db.StudentSubmissions
                    .Where(ss => ss.StudentAssignment.Enrollment.CourseId == data.CourseId)
                    .Where(ss => ss.StudentAssignment.Enrollment.UserId == user.Id)
                    .Where(ss => ss.StudentAssignment.AssignmentId == data.AssignmentId)
                    .Where(ss => ss.StudentSubmissionId == data.SubmissionId)
                    .Include(ss => ss.StudentAssignment.Assignment)
                    .Include(ss => ss.StudentAssignment.Enrollment)
                    .FirstOrDefault();

                if (submission == null)
                {
                    return new HttpNotFoundResult();
                }

                var repo = SubmissionRepository.Get(submission.StudentAssignment);
                repo.Checkout(submission.CommitId); // get selected
                var dir = TempDir.Get(repo);

                var folder = dir.GetFolder();

                return new JsonNetResult(new { Folder = folder, Results = submission.PreSubmissionResults });
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        public ActionResult Submit(IDEPostData data)
        {
            var repo = _GetRepo(data);
            if (repo == null)
            {
                return new HttpNotFoundResult();
            }
            try
            {
                // TODO: in the future manage issues with versions in this repo
                var submission = SubmissionManager.Submit(repo);
                return new JsonNetResult(submission);
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        public ActionResult RetestSubmission(SubmissionPostData data)
        {
            var course = _GetCourse();
            if (course == null || course.CourseId != data.CourseId)
            {
                return new HttpNotFoundResult();
            }

            var user = ApplicationUser.Current;
            if (user == null || user.Id != data.UserId)
            {
                return new HttpUnauthorizedResult();
            }

            try
            {
                var studentAssignment = _db.StudentAssignments
                    .Include(sa => sa.Submissions)
                    .Include(sa => sa.Assignment.Pages)
                    .Include(sa => sa.Enrollment.User)
                    .FirstOrDefault(sa => sa.AssignmentId == data.AssignmentId && sa.Enrollment.UserName == user.UserName);

                if (studentAssignment == null)
                {
                    return new HttpNotFoundResult();
                }

                StudentSubmission submission;
                if (data.SubmissionId > 0)
                {
                    submission = studentAssignment.Submissions.FirstOrDefault(s => s.StudentSubmissionId == data.SubmissionId);
                }
                else
                {
                    submission = studentAssignment.Submissions.LastOrDefault();
                }

                if (submission == null)
                {
                    return null;
                }

                using (var t = new SubmissionTester(submission))
                {
                    t.TestAll();
                }

                _db.SaveChanges();

                return new JsonNetResult(submission.PreSubmissionResults);
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Auger;
using Auger.DAL;
using Auger.Models.Data;
using Auger.Models.View;
using Auger.Models;

namespace Auger.Controllers
{
    public class CourseAdminController : Controller
    {
        private AugerContext _db = new AugerContext();

        // GET: CourseAdmin
        [Authorize]
        public ActionResult Index()
        {
            List<Course> courses = null;
            if (User.IsInRole(UserRoles.SuperUserRole))
            {
                courses = _db.Courses.ToList();
            }
            else
            {
                courses = new List<Course>();
                var userName = User.GetName();
                var enrollments = _db.Enrollments
                    .Include(e => e.Course)
                    .Where(e => e.UserName == userName);
                foreach (var e in enrollments)
                {
                    if (e.IsInRole(UserRoles.InstructorRole))
                    {
                        courses.Add(e.Course);
                    }
                }
            }
            CookieManager.ClearCourseId();
            return View(courses);
        }

        private bool IsCurrentCourse(int? id)
        {
            return id == CookieManager.GetCourseId();
        }

        private ActionResult RedirectToCourse(int? id)
        {
            CookieManager.SetCourseId(id.Value);
            return RedirectToAction("CourseDetails", "CourseAdmin", new { courseId = id.Value });
        }

        #region ///////////////// AJAX CALLS /////////////////////

        // POST: CourseAdmin/GetAllCourses
        [Authorize]
        [HttpPost]
        public JsonResult GetAllCourses()
        {
            var user = ApplicationUser.Current;
            var courses = _db.Enrollments.Where(e => e.UserId == user.Id).Select(e => e.Course);
            return new JsonNetResult(courses.ToList().OrderByDescending(c => c.DateCreated));
        }

        // POST: CourseAdmin/GetAssignmentsForCourse
        [Authorize]
        [HttpPost]
        public JsonResult GetAssignmentsForCourse(int courseId)
        {
            var user = ApplicationUser.Current;
            var course = _db.Enrollments
                .Where(e => e.UserId == user.Id && e.CourseId == courseId)
                .Select(e => e.Course).FirstOrDefault();
            if (course == null)
            {
                return new JsonNetResult();
            }
            return new JsonNetResult(course.Assignments.ToList());
        }

        #endregion

        #region /////////////// COURSE CRUD //////////////////

        // GET: CourseAdmin/CourseDetails/5
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult CourseDetails(int? courseId)
        {
            if (courseId == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (!IsCurrentCourse(courseId))
                return RedirectToCourse(courseId);

            Course course = _db.Courses.Find(courseId);
            if (course == null)
            {
                return HttpNotFound();
            }
            return View(course);
        }

        // GET: CourseAdmin/CourseLink
        [Authorize]
        public ActionResult CourseLink()
        {
            var ltiContext = TempData["ltiContext"] as LtiContextModel;
            var user = ApplicationUser.Current;
            var unlinkedCourses = _db.Enrollments.Where(e => e.UserId == user.Id && e.Course.LtiContextId == null).Select(e => e.Course);

            if (!unlinkedCourses.Any())
            {
                return HttpNotFound();
            }

            return View(unlinkedCourses);
        }

        // POST: CourseAdmin/CourseLink
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult CourseLink(string disposition, int? courseId)
        {
            if (disposition.Trim().ToLowerInvariant() == "create")
            {
                return RedirectToAction("CourseCreate", "CourseAdmin");
            }
            else
            {
                var course = _db.Courses.Find(courseId);
                var ltiContext = TempData["ltiContext"] as LtiContextModel;
                if (course == null || ltiContext == null)
                {
                    // TODO: Do better error handling here
                    return HttpNotFound();
                }

                course.LtiContextId = ltiContext.LtiContextId;
                _db.SaveChanges();

                CookieManager.SetCourseId(course.CourseId);

                // TODO: Handle new assignment
                return RedirectToAction("CourseDetails", new { courseId = course.CourseId });
            }
        }

        // GET: CourseAdmin/CourseCreate
        [Authorize]
        public ActionResult CourseCreate()
        {
            var course = new Course();

            var ltiContext = TempData["ltiContext"] as LtiContextModel;
            if (ltiContext != null && !ltiContext.IsCourseLinked)
            {
                course.LtiContextId = ltiContext.LtiContextId;
                course.CourseTitle = ltiContext.CourseTitle;
                course.CourseLabel = ltiContext.CourseLabel;
            }

            return View(course);
        }

        // POST: CourseAdmin/CourseCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult CourseCreate(Course course)
        {
            var ltiContext = TempData["ltiContext"] as LtiContextModel;

            if (ModelState.IsValid)
            {
                _db.Courses.Add(course);
                _db.SaveChanges();

                CookieManager.SetCourseId(course.CourseId);

                var user = ApplicationUser.Current;
                var roles = UserRoles.InstructorRole;
                if (ltiContext != null && !ltiContext.IsCourseLinked)
                {
                    ltiContext.IsCourseLinked = true;
                    roles = string.Join(",", ltiContext.Roles);
                }
                Enrollment e = new Enrollment
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    AllRoles = roles,
                    CourseId = course.CourseId,
                    IsActive = true
                };
                _db.Enrollments.Add(e);
                _db.SaveChanges();

                if (ltiContext != null && !ltiContext.IsAssignmentLinked)
                {
                    return RedirectToAction("AssignmentCreate", new { courseId = course.CourseId });
                }
                else
                {
                    return RedirectToAction("CourseDetails", new { courseId = course.CourseId });
                }
            }

            return View(course);
        }

        // GET: CourseAdmin/CourseEdit/5
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult CourseEdit(int? courseId)
        {
            if (courseId == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (!IsCurrentCourse(courseId))
                return RedirectToCourse(courseId);

            Course course = _db.Courses.Find(courseId);
            if (course == null)
            {
                return HttpNotFound();
            }
            return View(course);
        }

        // POST: CourseAdmin/CourseEdit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult CourseEdit(Course course)
        {
            if (ModelState.IsValid)
            {
                Course dbCourse = _db.Courses.Find(course.CourseId);
                if (dbCourse == null)
                {
                    return HttpNotFound();
                }
                dbCourse.CourseLabel = course.CourseLabel;
                dbCourse.CourseTitle = course.CourseTitle;
                dbCourse.IsActive = course.IsActive;
                _db.SaveChanges();
                return RedirectToAction("CourseDetails", new { courseId = course.CourseId });
            }
            return View(course);
        }

        // GET: CourseAdmin/CourseDelete/5
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult CourseDelete(int? courseId)
        {
            if (courseId == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (!IsCurrentCourse(courseId))
                return RedirectToCourse(courseId);

            Course course = _db.Courses.Find(courseId);
            if (course == null)
            {
                return HttpNotFound();
            }
            return View(course);
        }

        // POST: CourseAdmin/CourseDelete/5
        [HttpPost, ActionName("CourseDelete")]
        [ValidateAntiForgeryToken]
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult CourseDeleteConfirmed(int courseId)
        {
            Course course = _db.Courses.Find(courseId);
            _db.Courses.Remove(course);
            _db.SaveChanges();
            return RedirectToAction("Index");
        }

        #endregion

        #region /////////////// ASSIGNMENT CRUD //////////////////

        // GET: CourseAdmin/AssignmentDetails/5/6
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult AssignmentDetails(int? courseId, int? id)
        {
            if (courseId == null || id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (!IsCurrentCourse(courseId))
                return RedirectToCourse(courseId);

            Assignment assignment = _db.Assignments
                .Include(a => a.Course)
                .Include(a => a.AllScripts)
                .Include(a => a.Pages)
                .Include(a => a.StudentAssignments.Select(sa => sa.Enrollment))
                .FirstOrDefault(a => a.AssignmentId == id);
            if (assignment == null)
            {
                return HttpNotFound();
            }
            return View(assignment);
        }

        // GET: CourseAdmin/AssignmentLink/5
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult AssignmentLink(int courseId)
        {
            var unlinkedAssignments = _db.Assignments.Where(a => a.CourseId == courseId && a.LtiResourceLinkId == null);
            if (!unlinkedAssignments.Any())
            {
                return HttpNotFound();
            }

            return View(unlinkedAssignments);
        }

        // POST: CourseAdmin/AssignmentLink
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult AssignmentLink(string disposition, int courseId, int? assignmentId)
        {
            if (disposition.Trim().ToLowerInvariant() == "create")
            {
                return RedirectToAction("AssignmentCreate", "CourseAdmin", new { courseId = courseId });
            }
            else
            {
                var assignment = _db.Assignments.Find(assignmentId);
                var ltiContext = TempData["ltiContext"] as LtiContextModel;
                if (assignment == null || ltiContext == null)
                {
                    // TODO: Do better error handling here
                    return HttpNotFound();
                }

                assignment.LtiResourceLinkId = ltiContext.LtiResourceLinkId;
                _db.SaveChanges();

                return RedirectToAction("AssignmentDetails", new { courseId = courseId, id = assignmentId });
            }
        }


        // GET: CourseAdmin/AssignmentCreate/5
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult AssignmentCreate(int? courseId)
        {
            if (courseId == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (!IsCurrentCourse(courseId))
                return RedirectToCourse(courseId);

            var assignment = new Assignment
            {
                CourseId = courseId.Value
            };

            var ltiContext = TempData["ltiContext"] as LtiContextModel;
            if (ltiContext != null && !ltiContext.IsAssignmentLinked)
            {
                assignment.LtiResourceLinkId = ltiContext.LtiResourceLinkId;
                assignment.AssignmentName = ltiContext.AssignmentName;
            }
            return View(assignment);
        }

        // POST: CourseAdmin/AssignmentCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult AssignmentCreate(Assignment assignment)
        {
            if (ModelState.IsValid)
            {
                _db.Assignments.Add(assignment);
                _db.SaveChanges();

                foreach (var enrollment in _db.Enrollments.Where(e => e.CourseId == assignment.CourseId))
                {
                    _db.StudentAssignments.Add(
                        new StudentAssignment
                        {
                            AssignmentId = assignment.AssignmentId,
                            EnrollmentId = enrollment.EnrollmentId
                        }
                    );
                }
                _db.SaveChanges();

                var ltiContext = TempData["ltiContext"] as LtiContextModel;
                if (ltiContext != null && !ltiContext.IsAssignmentLinked)
                {
                    ltiContext.IsAssignmentLinked = true;
                }
                return RedirectToAction("AssignmentDetails", new { courseId = assignment.CourseId, id = assignment.AssignmentId });
            }

            return View(assignment);
        }

        // GET: CourseAdmin/AssignmentEdit/5
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult AssignmentEdit(int? courseId, int? id)
        {
            if (courseId == null || id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (!IsCurrentCourse(courseId))
                return RedirectToCourse(courseId);

            Assignment assignment = _db.Assignments.Find(id);
            if (assignment == null)
            {
                return HttpNotFound();
            }
            return View(assignment);
        }

        // POST: CourseAdmin/AssignmentEdit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult AssignmentEdit(Assignment assignment)
        {
            if (ModelState.IsValid)
            {
                Assignment dbAssignment = _db.Assignments.Find(assignment.AssignmentId);
                if (dbAssignment == null)
                {
                    return HttpNotFound();
                }
                dbAssignment.AssignmentName = assignment.AssignmentName;
                dbAssignment.DueDate = assignment.DueDate;
                _db.SaveChanges();
                return RedirectToAction("AssignmentDetails", new { courseId = assignment.CourseId, id = assignment.AssignmentId });
            }
            return View(assignment);
        }

        // GET: CourseAdmin/AssignmentDelete/5
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult AssignmentDelete(int? courseId, int? id)
        {
            if (courseId == null || id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (!IsCurrentCourse(courseId))
                return RedirectToCourse(courseId);

            Assignment assignment = _db.Assignments.Find(id);
            if (assignment == null)
            {
                return HttpNotFound();
            }
            return View(assignment);
        }

        // POST: CourseAdmin/AssignmentDelete/5
        [HttpPost, ActionName("AssignmentDelete")]
        [ValidateAntiForgeryToken]
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult AssignmentDeleteConfirmed(int? courseId, int id)
        {
            _db.StudentAssignments.RemoveRange(_db.StudentAssignments.Where(sa => sa.AssignmentId == id));
            Assignment assignment = _db.Assignments.Find(id);
            _db.Assignments.Remove(assignment);
            _db.SaveChanges();
            return RedirectToAction("CourseDetails", new { courseId = assignment.CourseId });
        }

        #endregion

        #region //////////// ASSIGNMENT IMPORT //////////////

        // POST: CourseAdmin/AssignmentImport
        [HttpPost]
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult AssignmentImport(int courseId, int assignmentId, int sourceAssignmentId)
        {
            if (!IsCurrentCourse(courseId))
                return new JsonNetResult("Not In Course");

            var course = _db.Courses.Find(courseId);
            var thisAssignment = course.Assignments.FirstOrDefault(a => a.AssignmentId == assignmentId);
            var otherAssignment = _db.Assignments.Find(sourceAssignmentId);

            if (thisAssignment == null || otherAssignment == null)
                return new JsonNetResult("Unable to find the requested assignment");

            // DELETE EVERYTHING
            foreach (var page in thisAssignment.Pages)
            {
                _db.Scripts.RemoveRange(page.AllScripts);
            }
            _db.Scripts.RemoveRange(thisAssignment.AllScripts);
            _db.Pages.RemoveRange(thisAssignment.Pages);
            _db.SaveChanges();

            // COPY THE OTHER ASSIGNMENT
            foreach (var otherPage in otherAssignment.Pages)
            {
                var newPage = new Page()
                {
                    PageName = otherPage.PageName
                };
                thisAssignment.Pages.Add(newPage);
                _db.SaveChanges();

                foreach (var otherScript in otherPage.AllScripts)
                {
                    var newScript = new Script()
                    {
                        AssignmentId = newPage.AssignmentId,
                        PageId = newPage.PageId,
                        DeviceId = otherScript.DeviceId,
                        IsPreGrade = otherScript.IsPreGrade,
                        ScriptName = otherScript.ScriptName,
                        ScriptText = otherScript.ScriptText
                    };
                    _db.Scripts.Add(newScript);
                }
                _db.SaveChanges();
            }
            foreach (var otherScript in otherAssignment.CommonScripts)
            {
                var newScript = new Script()
                {
                    DeviceId = otherScript.DeviceId,
                    IsPreGrade = otherScript.IsPreGrade,
                    ScriptName = otherScript.ScriptName,
                    ScriptText = otherScript.ScriptText
                };
                thisAssignment.AllScripts.Add(newScript);
            }
            _db.SaveChanges();

            return new JsonNetResult(true);
        }

        #endregion

        #region /////////////// PAGE CRUD //////////////////

        // GET: CourseAdmin/PageDetails/5
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult PageDetails(int? courseId, int? id)
        {
            if (courseId == null || id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (!IsCurrentCourse(courseId))
                return RedirectToCourse(courseId);

            Page page = _db.Pages
                .Include(a => a.AllScripts)
                .Include(a => a.Assignment)
                .Include(a => a.Assignment.Course)
                .FirstOrDefault(a => a.PageId == id);
            if (page == null)
            {
                return HttpNotFound();
            }
            return View(page);
        }


        // GET: CourseAdmin/PageCreate/5
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult PageCreate(int? courseId, int? id)
        {
            if (courseId == null || id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (!IsCurrentCourse(courseId))
                return RedirectToCourse(courseId);

            var page = new Page
            {
                AssignmentId = id.Value
            };
            return View(page);
        }

        // POST: CourseAdmin/PageCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult PageCreate(Page page)
        {
            if (ModelState.IsValid)
            {
                _db.Pages.Add(page);
                _db.SaveChanges();
                var assignment = _db.Assignments.Find(page.AssignmentId);
                return RedirectToAction("PageDetails", new { courseId = assignment.CourseId, id = page.PageId });
            }

            return View(page);
        }

        // GET: CourseAdmin/PageEdit/5
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult PageEdit(int? courseId, int? id)
        {
            if (courseId == null || id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (!IsCurrentCourse(courseId))
                return RedirectToCourse(courseId);

            Page page = _db.Pages.Find(id);
            if (page == null)
            {
                return HttpNotFound();
            }
            return View(page);
        }

        // POST: CourseAdmin/PageEdit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult PageEdit(Page page)
        {
            if (ModelState.IsValid)
            {
                Page dbPage = _db.Pages.Include(p => p.Assignment).FirstOrDefault(p => p.PageId == page.PageId);
                if (dbPage == null)
                {
                    return HttpNotFound();
                }
                dbPage.PageName = page.PageName;
                _db.SaveChanges();
                return RedirectToAction("PageDetails", new { courseId = dbPage.Assignment.CourseId, id = page.PageId });
            }
            return View(page);
        }

        // GET: CourseAdmin/PageDelete/5
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult PageDelete(int? courseId, int? id)
        {
            if (courseId == null || id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (!IsCurrentCourse(courseId))
                return RedirectToCourse(courseId);

            Page page = _db.Pages.Find(id);
            if (page == null)
            {
                return HttpNotFound();
            }
            return View(page);
        }

        // POST: CourseAdmin/PageDelete/5
        [HttpPost, ActionName("PageDelete")]
        [ValidateAntiForgeryToken]
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult PageDeleteConfirmed(int? courseId, int id)
        {
            Page page = _db.Pages.Include(p => p.Assignment).FirstOrDefault(p => p.PageId == id);
            _db.Pages.Remove(page);
            _db.SaveChanges();
            var assignment = _db.Assignments.Find(page.AssignmentId);
            return RedirectToAction("AssignmentDetails", new { courseId = assignment.CourseId, id = page.AssignmentId });
        }

        #endregion

        #region /////////////// ENROLLMENT //////////////////

        // GET: CourseAdmin/EnrollmentDetails/5/6
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult EnrollmentDetails(int? courseId, int? id)
        {
            if (courseId == null || id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (!IsCurrentCourse(courseId))
                return RedirectToCourse(courseId);

            Enrollment enrollment = _db.Enrollments
                .Include(e => e.Course)
                .Include(e => e.User)
                .Include(e => e.StudentAssignments.Select(sa => sa.Assignment))
                .FirstOrDefault(e => e.EnrollmentId == id);
            if (enrollment == null)
            {
                return HttpNotFound();
            }
            return View(enrollment);
        }

        // GET: CourseAdmin/EnrollmentEdit/5/6
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult EnrollmentEdit(int? courseId, int? id)
        {
            if (courseId == null || id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (!IsCurrentCourse(courseId))
                return RedirectToCourse(courseId);

            Enrollment enrollment = _db.Enrollments.Find(id);
            if (enrollment == null)
            {
                return HttpNotFound();
            }
            return View(enrollment);
        }

        // POST: CourseAdmin/EnrollmentEdit/5/6
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult EnrollmentEdit(Enrollment enrollment)
        {
            if (ModelState.IsValid)
            {
                Enrollment dbEnrollment = _db.Enrollments.Find(enrollment.EnrollmentId);
                if (dbEnrollment == null)
                {
                    return HttpNotFound();
                }
                dbEnrollment.IsActive = enrollment.IsActive;
                _db.SaveChanges();
                return RedirectToAction("EnrollmentDetails", new { courseId = enrollment.CourseId, id = enrollment.EnrollmentId });
            }
            return View(enrollment);
        }

        // GET: CourseAdmin/EnrollmentDelete/5/6
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult EnrollmentDelete(int? courseId, int? id)
        {
            if (courseId == null || id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (!IsCurrentCourse(courseId))
                return RedirectToCourse(courseId);

            Enrollment enrollment = _db.Enrollments.Find(id);
            if (enrollment == null)
            {
                return HttpNotFound();
            }
            return View(enrollment);
        }

        // POST: CourseAdmin/AssignmentDelete/5/6
        [HttpPost, ActionName("EnrollmentDelete")]
        [ValidateAntiForgeryToken]
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult EnrollmentDeleteConfirmed(int? courseId, int id)
        {
            _db.StudentAssignments.RemoveRange(_db.StudentAssignments.Where(sa => sa.EnrollmentId == id));
            Enrollment enrollment = _db.Enrollments.Find(id);
            _db.Enrollments.Remove(enrollment);
            _db.SaveChanges();
            return RedirectToAction("CourseDetails", new { courseId = courseId });
        }

        #endregion



        #region ////////////////// STUDENTASSIGNMENT ///////////////////

        // GET: CourseAdmin/StudentAssignmentDetails/5/6
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult StudentAssignmentDetails(int? courseId, int? id, int? selectedId = null)
        {
            if (courseId == null || id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (!IsCurrentCourse(courseId))
                return RedirectToCourse(courseId);

            var studentAssignment = _db.StudentAssignments
                .Include(sa => sa.Assignment.Course)
                .Include(sa => sa.Submissions)
                .Include(sa => sa.Enrollment)
                .FirstOrDefault(sa => sa.StudentAssignmentId == id);
            if (studentAssignment == null)
            {
                return HttpNotFound();
            }

            StudentSubmission submission = studentAssignment.Submissions.LastOrDefault();
            if (selectedId.HasValue)
            {
                submission = studentAssignment.Submissions.FirstOrDefault(s => s.StudentSubmissionId == selectedId);
            }

            SubmissionViewModel svm = null;
            if (submission != null)
            {
                var repo = SubmissionRepository.Get(studentAssignment);
                repo.CheckoutSubmission(submission.CommitId);
                var work = TempDir.Get(repo);
                svm = new SubmissionViewModel
                {
                    Submission = submission,
                    Folder = SubmissionManager.GetFolder(submission)
                };
                repo.CheckoutSubmission();
            }

            var allAssignments = _db.StudentAssignments
                .Include(sa => sa.Enrollment)
                .Where(sa => sa.AssignmentId == studentAssignment.AssignmentId)
                .Where(sa => sa.Submissions.Count > 0)
                .AsEnumerable();

            var vm = new StudentAssignmentDetailsViewModel
            {
                StudentAssignment = studentAssignment,
                SelectedSubmission = svm,
                AllAssignments = allAssignments
            };

            return View(vm);
        }

        // POST: Grade/RetestSubmission/courseId/submissionId
        [HttpPost]
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult RetestSubmission(int? courseId, int? id)
        {
            if (courseId == null || id == null)
                return Json(false);
            if (!IsCurrentCourse(courseId))
                return Json(false);

            try
            {
                var submission = _db.StudentSubmissions
                    .Include(ss => ss.StudentAssignment.Assignment.Pages)
                    .Include(ss => ss.StudentAssignment.Enrollment.User)
                    .FirstOrDefault(ss => ss.StudentSubmissionId == id);

                if (submission == null)
                    return Json(false);

                var repo = SubmissionRepository.Get(submission.StudentAssignment);
                repo.CheckoutSubmission(submission.CommitId);

                SubmissionTester.TestSubmission(submission);
                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return Json(false);
            }

            return Json(true);
        }

        #endregion


        #region /////////////// SCRIPTS //////////////////

        // GET: CourseAdmin/AssignmentCreate/5/6/7?
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult ScriptCreate(int? courseId, int? id, int? selectedId = null)
        {
            if (courseId == null || id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (!IsCurrentCourse(courseId))
                return RedirectToCourse(courseId);

            var assignment = _db.Assignments
                .Include(a => a.Pages)
                .Include(a => a.Course)
                .FirstOrDefault(a => a.AssignmentId == id);

            var script = new Script
            {
                Assignment = assignment,
                AssignmentId = id,
                PageId = selectedId
            };

            ViewBag.Pages = assignment.Pages.AsEnumerable(); ;

            return View(script);
        }

        // POST: CourseAdmin/ScriptCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult ScriptCreate(Script script)
        {
            if (ModelState.IsValid)
            {
                _db.Scripts.Add(script);
                _db.SaveChanges();

                var assignment = _db.Assignments.Find(script.AssignmentId);

                return RedirectToAction("ManageScripts", new { courseId = assignment.CourseId, id = assignment.AssignmentId, selectedId=script.ScriptId });
            }

            return View(script);
        }

        // GET: CourseAdmin/ScriptEdit/5/6
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult ScriptEdit(int? courseId, int? id)
        {
            if (courseId == null || id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (!IsCurrentCourse(courseId))
                return RedirectToCourse(courseId);

            Script script = _db.Scripts
                .Include(s => s.Page)
                .Include(s => s.Assignment)
                .FirstOrDefault(s => s.ScriptId == id);
            if (script == null)
            {
                return HttpNotFound();
            }
            return View(script);
        }

        // POST: CourseAdmin/ScriptEdit/5/6
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult ScriptEdit(Script script)
        {
            if (ModelState.IsValid)
            {
                Script dbScript = _db.Scripts
                    .Include(s => s.Assignment)
                    .FirstOrDefault(s => s.ScriptId == script.ScriptId);
                if (dbScript == null)
                {
                    return HttpNotFound();
                }
                dbScript.ScriptName = script.ScriptName;
                dbScript.IsPreGrade = script.IsPreGrade;
                dbScript.DeviceId = script.DeviceId;
                _db.SaveChanges();

                return RedirectToAction("ManageScripts", new { courseId = dbScript.Assignment.CourseId, id = dbScript.AssignmentId, selectedId = dbScript.ScriptId });
            }
            return View(script);
        }

        // GET: CourseAdmin/ScriptDelete/5/6
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult ScriptDelete(int? courseId, int? id)
        {
            if (courseId == null || id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (!IsCurrentCourse(courseId))
                return RedirectToCourse(courseId);

            Script script = _db.Scripts.Find(id);
            if (script == null)
            {
                return HttpNotFound();
            }
            return View(script);
        }

        // POST: CourseAdmin/ScriptDelete/5/6
        [HttpPost, ActionName("ScriptDelete")]
        [ValidateAntiForgeryToken]
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult ScriptDeleteConfirmed(int? courseId, int id)
        {
            Script script = _db.Scripts.FirstOrDefault(s => s.ScriptId == id);

            var assignmentId = script.AssignmentId;

            _db.Scripts.Remove(script);
            _db.SaveChanges();

            return RedirectToAction("ManageScripts", new { courseId = courseId, id = assignmentId });
        }

        // GET: CourseAdmin/ManageScripts/5/6/7?
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult ManageScripts(int? courseId, int? id, int? selectedId = null)
        {
            if (courseId == null || id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (!IsCurrentCourse(courseId))
                return RedirectToCourse(courseId);

            Assignment assignment = _db.Assignments
                .Include(a => a.Course)
                .Include(a => a.AllScripts)
                .Include(a => a.Pages.Select(p => p.AllScripts))
                .Include(a => a.StudentAssignments.Select(sa => sa.Enrollment))
                .FirstOrDefault(a => a.AssignmentId == id);
            if (assignment == null)
            {
                return new HttpNotFoundResult();
            }
            ViewBag.SelectedScriptId = selectedId ?? assignment.AllScripts.FirstOrDefault()?.ScriptId;
            return View(assignment);
        }

        // GET: CourseAdmin/ScriptSource/5/6
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult ScriptSource(int? courseId, int? id)
        {
            if (courseId == null || id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (!IsCurrentCourse(courseId))
                return RedirectToCourse(courseId);

            var script = _db.Scripts.FirstOrDefault(s => s.ScriptId == id);

            if (script == null)
                return new HttpNotFoundResult();

            return new JavaScriptResult
            {
                Script = script.ScriptText
            };
        }

        // POST: CourseAdmin/ScriptSource/5/6
        [ValidateInput(false)]
        [HttpPost, ActionName("ScriptSource")]
        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult ScriptSourcePost(int? courseId, int? id, string source)
        {
            if (courseId == null || id == null)
                return Json(false);
            if (!IsCurrentCourse(courseId))
                return Json(false);

            try
            {
                var script = _db.Scripts.FirstOrDefault(s => s.ScriptId == id);

                if (script == null)
                    return Json(false);

                script.ScriptText = source;
                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return Json(false);
            }

            return Json(true);
        }

        #endregion








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

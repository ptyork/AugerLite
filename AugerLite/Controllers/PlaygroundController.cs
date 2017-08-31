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
    public class PlaygroundController : IDEControllerBase<PlaygroundRepository>
    {
        public ActionResult Index()
        {
            Course course = _GetCourse();
            if (course == null)
            {
                return RedirectToAction("SelectCourse");
            }

            try
            {
                PlaygroundIndexViewModel vm = new PlaygroundIndexViewModel();
                vm.Course = course;

                var user = ApplicationUser.Current;

                vm.Playgrounds = PlaygroundRepository.GetAllPlaygrounds(course.CourseId, user.UserName);

                return View(vm);
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        public ActionResult ClearCourse()
        {
            CookieManager.ClearCourseId();
            return RedirectToAction("Index");
        }

        public ActionResult SelectCourse(int id = 0)
        {
            try
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
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        public class PlaygroundName
        {
            [AllowHtml]
            public string Name { get; set; }
        }

        public ActionResult Create(PlaygroundName pgn)
        {
            Course course = _GetCourse();
            if (course == null)
            {
                return RedirectToAction("SelectCourse");
            }

            try
            {
                var repo = PlaygroundRepository.Create(course.CourseId, User.GetName(), pgn.Name);
                return RedirectToAction("Edit", new { id = repo.RepositoryId });
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        public ActionResult Rename(int id, PlaygroundName pgn)
        {
            Course course = _GetCourse();
            if (course == null)
            {
                return RedirectToAction("SelectCourse");
            }

            var repo = PlaygroundRepository.Get(course.CourseId, User.GetName(), id, false);
            if (repo == null)
            {
                return new HttpNotFoundResult();
            }

            try
            {
                repo.SetName(pgn.Name);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        public ActionResult Delete(int id)
        {
            Course course = _GetCourse();
            if (course == null)
            {
                return RedirectToAction("SelectCourse");
            }

            var repo = PlaygroundRepository.Get(course.CourseId, User.GetName(), id, false);
            if (repo == null)
            {
                return new HttpNotFoundResult();
            }

            try
            {
                PlaygroundRepository.Delete(repo);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        public ActionResult Edit(int id)
        {
            Course course = _GetCourse();
            if (course == null)
            {
                return RedirectToAction("SelectCourse");
            }

            var user = ApplicationUser.Current;

            var playground = PlaygroundRepository.GetPlayground(course.CourseId, user.UserName, id);

            try
            {
                var model = new PlaygroundEditViewModel()
                {
                    Course = course,
                    User = user,
                    Playground = playground
                };

                return View(model);
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
    }
}
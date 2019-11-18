using Auger.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Auger.Controllers
{
    [OutputCache(NoStore = true, Duration = 0)]
    public class BrowseController : AugerControllerBase
    {
        private static string[] defaultFiles =
        {
            "index.html",
            "index.htm",
            "default.html",
            "default.htm",
            "home.html",
            "home.htm"
        };

        [Authorize]
        public ActionResult BrowseWork(int courseId, int assignmentId, string pathInfo)
        {
            return _Browse(WorkRepository.Get(courseId, User.GetName(), assignmentId).FilePath, pathInfo);
        }

        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult BrowseUserWork(int courseId, string userName, int assignmentId, string pathInfo)
        {
            return _Browse(WorkRepository.Get(courseId, userName, assignmentId).FilePath, pathInfo);
        }

        [Authorize]
        public ActionResult BrowsePlay(int courseId, string userName, int playgroundId, string pathInfo)
        {
            var repo = PlaygroundRepository.Get(courseId, userName, playgroundId);
            var user = ApplicationUser.Current;

            // if this is the user's own repository OR the user is a super user OR the repository is shared,
            // then the repository should be browsable
            if (string.Equals(user.UserName, userName, StringComparison.OrdinalIgnoreCase) || user.IsInRole(UserRoles.SuperUserRole) || repo.GetIsShared())
            {
                return _Browse(repo.FilePath, pathInfo);
            }
            // also allow instructors to view any repo for their courses
            try
            {
                var course = _GetCourse(courseId);
                if (course != null && user.IsInstructorForCourse(course))
                {
                    return _Browse(repo.FilePath, pathInfo);
                }
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
            }
            return new HttpNotFoundResult();
        }

        [Authorize]
        public ActionResult BrowseSelf(int courseId, int assignmentId, string pathInfo)
        {
            return _Browse(TempDir.GetPath(courseId, User.GetName(), assignmentId), pathInfo);
        }

        [CourseAuthorize(UserRoles.InstructorRole)]
        public ActionResult BrowseUser(int courseId, string userName, int assignmentId, string pathInfo)
        {
            return _Browse(TempDir.GetPath(courseId, userName, assignmentId), pathInfo);
        }

        private ActionResult _Browse(string basePath, string pathInfo)
        {
            pathInfo = pathInfo ?? "";
            pathInfo = pathInfo.Replace('/', '\\');

            var fullPath = $"{basePath}\\{pathInfo}";

            if (System.IO.File.Exists(fullPath))
            {
                return File(fullPath, MimeMapping.GetMimeMapping(fullPath));
            }
            else
            {
                foreach (var def in defaultFiles)
                {
                    var test = $"{fullPath}\\{def}";
                    if (System.IO.File.Exists(test))
                    {
                        return File(test, MimeMapping.GetMimeMapping(test));
                    }
                }
            }
            return new HttpNotFoundResult();
        }

        public async Task<ActionResult> BrowseAny(string url)
        {
            HttpClient client = new HttpClient();
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var type = response.Content.Headers.ContentType.MediaType;
                return new FileContentResult(await response.Content.ReadAsByteArrayAsync(), type);
            }
            else
            {
                return new HttpNotFoundResult();
            }
        }
    }
}
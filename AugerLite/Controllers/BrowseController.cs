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
    public class BrowseController : Controller
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
        public ActionResult BrowseSelf(string courseId, string assignmentId, string pathInfo)
        {
            var userId = User.GetName();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new HttpUnauthorizedResult();
            }
            return BrowseUser(courseId, userId, assignmentId, pathInfo);
        }

        [Authorize]
        public ActionResult BrowseUser(string courseId, string userId, string assignmentId, string pathInfo)
        {
            pathInfo = pathInfo ?? "";
            pathInfo = pathInfo.Replace('/', '\\');

            //var basePath = Repository.FolderName;
            //var file = $"{basePath}\\{courseId}\\{userId}\\{assignmentId}\\{pathInfo}";
            var file = $"{TempDir.GetPath(courseId, userId, assignmentId)}\\{pathInfo}";
            var segments = pathInfo.Split('\\');
            if (segments.Length > 0 && segments.Last().Contains('.'))
            {
                if (System.IO.File.Exists(file))
                {
                    return File(file, MimeMapping.GetMimeMapping(file));
                }
            }
            else
            {
                foreach (var def in defaultFiles)
                {
                    var test = $"{file}\\{def}";
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
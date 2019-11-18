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
    public abstract class IDEControllerBase<T> : AugerControllerBase
        where T : Repository
    {
        protected Repository _GetRepo(IDEPostData data)
        {
            Repository repo = null;
            try
            {
                var course = _GetCourse();
                if (course?.CourseId != data.CourseId)
                {
                    return null;
                }

                repo = (T)Activator.CreateInstance(typeof(T), data.CourseId, data.UserName, data.RepositoryId, true);

                var user = ApplicationUser.Current;
                if (user == null)
                {
                    return null;
                }

                // if this isn't the current user's repository and the user does not have sufficient rights,
                // then the repository must be shared
                if (!string.Equals(user.UserName, data.UserName, StringComparison.OrdinalIgnoreCase) && !user.IsInRole(UserRoles.SuperUserRole) && !user.IsInstructorForCourse(course))
                {
                    var pg = repo as PlaygroundRepository;
                    if (pg != null)
                    {
                        if (!pg.GetIsShared())
                        {
                            return null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }

                // if we get here, all is well so return the repo
                return repo;
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
            }
            return null;
        }

        [HttpPost]
        public ActionResult GetFolder(IDEPostData data)
        {
            var repo = _GetRepo(data);
            if (repo == null)
            {
                string message = $"Unable to find repo for {data.CourseId} / {data.UserName} / {data.RepositoryId}";
                Elmah.ErrorSignal.FromCurrentContext().Raise(new System.IO.FileNotFoundException(message));
                return new HttpStatusCodeResult(HttpStatusCode.NotFound, message);
            }
            try
            {
                // TODO: in the future manage issues with versions in this repo
                var folder = repo.GetFolder();
                return new JsonNetResult(folder);
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        public class ImportableProjects
        {
            public static class Types
            {
                public const string PLAYGROUND = "playground";
                public const string WORKSPACE = "workspace";
                public const string SUBMISSION = "submission";
            }

            public class ImportableProject
            {
                public string Type { get; set; }
                public int CourseId { get; set; }
                public int RepositoryId { get; set; }
                public string Name { get; set; }
            }

            public List<ImportableProject> Playgrounds { get; set; } = new List<ImportableProject>();
            public List<ImportableProject> AssignmentWorkspaces { get; set; } = new List<ImportableProject>();
            public List<ImportableProject> AssignmentSubmissions { get; set; } = new List<ImportableProject>();
        }

        [HttpPost]
        public ActionResult GetImportableProjects(IDEPostData data)
        {
            Course course = _GetCourse();
            if (course?.CourseId != data.CourseId)
            {
                return new HttpNotFoundResult();
            }

            if (User?.GetName()?.ToLowerInvariant() != data.UserName.ToLowerInvariant())
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "Attempt to retrieve importable projects from another user.");
            }

            try
            {
                var projects = new ImportableProjects();

                var playgrounds = PlaygroundRepository.GetAllPlaygrounds(data.CourseId, data.UserName);
                foreach (var playground in playgrounds)
                {
                    if (typeof(T) == typeof(PlaygroundRepository) && playground.PlaygroundId == data.RepositoryId)
                    {
                        continue;
                    }
                    projects.Playgrounds.Add(new ImportableProjects.ImportableProject()
                    {
                        Type = ImportableProjects.Types.PLAYGROUND,
                        CourseId = playground.CourseId,
                        RepositoryId = playground.PlaygroundId,
                        Name = playground.Name
                    });
                }

                var assignments = _db.StudentAssignments
                    .Include(sa => sa.Assignment.Course)
                    .Where(sa => sa.Enrollment.UserName == data.UserName) // CASE INSENSITIVE
                    .OrderByDescending(sa => sa.Assignment.DateCreated);
                foreach (var assignment in assignments)
                {
                    var name = assignment.Assignment.AssignmentName;
                    if (course.CourseId != assignment.Assignment.CourseId)
                    {
                        name += $" ({assignment.Assignment.Course.ShortName})";
                    }
                    if (typeof(T) != typeof(WorkRepository) || assignment.AssignmentId.Value != data.RepositoryId)
                    {
                        if (WorkRepository.Exists(course.CourseId, data.UserName, data.RepositoryId))
                        {
                            projects.AssignmentWorkspaces.Add(new ImportableProjects.ImportableProject()
                            {
                                Type = ImportableProjects.Types.WORKSPACE,
                                CourseId = assignment.Assignment.CourseId,
                                RepositoryId = assignment.AssignmentId.Value,
                                Name = name
                            });
                        }
                    }
                    if (assignment.HasSubmission)
                    {
                        if (typeof(T) != typeof(SubmissionRepository) || assignment.AssignmentId.Value != data.RepositoryId)
                        {
                            projects.AssignmentSubmissions.Add(new ImportableProjects.ImportableProject()
                            {
                                Type = ImportableProjects.Types.SUBMISSION,
                                CourseId = assignment.Assignment.CourseId,
                                RepositoryId = assignment.AssignmentId.Value,
                                Name = name
                            });
                        }
                    }
                }

                return new JsonNetResult(projects);
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        public class ProjectImportPostData : IDEPostData
        {
            public string SourceType { get; set; }
            public int SourceCourseId { get; set; }
            public string SourceUserName { get; set; }
            public int SourceRepositoryId { get; set; }
        }

        [HttpPost]
        public ActionResult ImportProject(ProjectImportPostData data)
        {
            var repo = _GetRepo(data);
            if (repo == null)
            {
                return new HttpNotFoundResult();
            }

            Repository sourceRepo = null;
            int courseId = data.SourceCourseId > 0 ? data.SourceCourseId : data.CourseId;
            if (data.SourceType == ImportableProjects.Types.PLAYGROUND)
            {
                string userName = String.IsNullOrWhiteSpace(data.SourceUserName) ? data.UserName : data.SourceUserName;
                sourceRepo = PlaygroundRepository.Get(courseId, userName, data.SourceRepositoryId);
            }
            else if (data.SourceType == ImportableProjects.Types.WORKSPACE)
            {
                sourceRepo = WorkRepository.Get(courseId, data.UserName, data.SourceRepositoryId);
            }
            else if (data.SourceType == ImportableProjects.Types.SUBMISSION)
            {
                sourceRepo = SubmissionRepository.Get(courseId, data.UserName, data.SourceRepositoryId);
                sourceRepo.Checkout();
            }
            if (sourceRepo == null)
            {
                return new HttpNotFoundResult();
            }

            try
            {
                repo.Commit("Before Import Project");
                repo.CopyFromRepository(sourceRepo);
                repo.Commit("After Import Project");
                return Json("success");
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        public class TextPostData : IDEPostData
        {
            public string Path { get; set; } = "";
            [AllowHtml]
            public string Text { get; set; } = "";
        }

        [HttpPost]
        public ActionResult FileReadText(TextPostData data)
        {
            var repo = _GetRepo(data);
            if (repo == null)
            {
                return new HttpNotFoundResult();
            }
            try
            {
                // TODO: in the future manage issues with versions in this repo
                var fullPath = repo.GetFullPath(data.Path);

                if (System.IO.File.Exists(fullPath))
                {
                    return File(fullPath, MimeMapping.GetMimeMapping(fullPath));
                }
                return new HttpNotFoundResult();
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        public ActionResult FileWriteText(TextPostData data)
        {
            var repo = _GetRepo(data);
            if (repo == null)
            {
                return new HttpNotFoundResult();
            }
            try
            {
                // TODO: in the future manage issues with versions in this repo
                var fullPath = repo.GetFullPath(data.Path);

                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.WriteAllText(fullPath, data.Text);
                    return Json("success");
                }
                return new HttpNotFoundResult();
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        public class NewFilePostData : IDEPostData
        {
            public string Name { get; set; } = "";
            public string Path { get; set; } = "";
            public string Template { get; set; } = "";
            public bool Confirm { get; set; } = false;
        }

        [HttpPost]
        public ActionResult NewFile(NewFilePostData data)
        {
            var repo = _GetRepo(data);
            if (repo == null)
            {
                return new HttpNotFoundResult();
            }
            try
            {
                // TODO: in the future manage issues with versions in this repo
                var fullPath = repo.GetFullPath(data.Path, data.Name);

                if (System.IO.File.Exists(fullPath) && !data.Confirm)
                {
                    return Json("conflict");
                }
                else
                {
                    var template = TemplateManager.GetTemplate(data.CourseId, data.Template);
                    if (template != null)
                    {
                        template.CopyTo(fullPath);
                    }
                    else
                    {
                        System.IO.File.WriteAllText(fullPath, "");
                    }
                    return Json("success");
                }
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        public class NewFolderPostData : IDEPostData
        {
            public string Name { get; set; } = "";
            public string Path { get; set; } = "";
            public bool Confirm { get; set; } = false;
        }

        [HttpPost]
        public ActionResult NewFolder(NewFolderPostData data)
        {
            var repo = _GetRepo(data);
            if (repo == null)
            {
                return new HttpNotFoundResult();
            }
            try
            {
                // TODO: in the future manage issues with versions in this repo
                var fullPath = repo.GetFullPath(data.Path, data.Name);

                if (System.IO.Directory.Exists(fullPath) && !data.Confirm)
                {
                    return Json("conflict");
                }
                else
                {
                    System.IO.Directory.CreateDirectory(fullPath);
                    return Json("success");
                }
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        public class FilePostData : IDEPostData
        {
            public string Name { get; set; } = "";
            public string Path { get; set; } = "";
            public string NewName { get; set; } = "";
            public string NewPath { get; set; } = "";
            public bool Confirm { get; set; } = false;
            public HttpPostedFileBase File { get; set; } = null;
        }

        [HttpPost]
        public ActionResult FileMove(FilePostData data)
        {
            var repo = _GetRepo(data);
            if (repo == null)
            {
                return new HttpNotFoundResult();
            }
            try
            {
                // TODO: in the future manage issues with versions in this repo
                var oldPath = repo.GetFullPath(data.Path, data.Name);
                var newBasePath = repo.GetFullPath(data.NewPath);
                var newFullPath = repo.GetFullPath(data.NewPath, data.NewName);

                var isFolder = false;

                if (!System.IO.File.Exists(oldPath))
                {
                    if (!System.IO.Directory.Exists(oldPath))
                    {
                        return new HttpNotFoundResult();
                    }
                    isFolder = true;
                }

                if (isFolder)
                {
                    if (!System.IO.Directory.Exists(newBasePath))
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Destination path does not exist");
                    }
                    else
                    {
                        if (System.IO.Directory.Exists(newFullPath) && !data.Confirm)
                        {
                            return Json("conflict");
                        }
                        else
                        {
                            var oldFolder = System.IO.Directory.CreateDirectory(oldPath);
                            oldFolder.MoveTo(newFullPath);
                            return Json("success");
                        }
                    }
                }
                else
                {
                    if (System.IO.File.Exists(newFullPath) && !data.Confirm)
                    {
                        return Json("conflict");
                    }
                    else
                    {
                        System.IO.File.Move(oldPath, newFullPath);
                        return Json("success");
                    }
                }
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        public ActionResult FileCopy(FilePostData data)
        {
            var repo = _GetRepo(data);
            if (repo == null)
            {
                return new HttpNotFoundResult();
            }
            try
            {
                // TODO: in the future manage issues with versions in this repo
                var oldPath = repo.GetFullPath(data.Path, data.Name);
                var newBasePath = repo.GetFullPath(data.NewPath);
                var newFullPath = repo.GetFullPath(data.NewPath, data.NewName);

                var isFolder = false;

                if (!System.IO.File.Exists(oldPath))
                {
                    if (!System.IO.Directory.Exists(oldPath))
                    {
                        return new HttpNotFoundResult();
                    }
                    isFolder = true;
                }

                if (isFolder)
                {
                    if (!System.IO.Directory.Exists(newBasePath))
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Destination path does not exist");
                    }
                    else
                    {
                        var oldFolder = System.IO.Directory.CreateDirectory(oldPath);
                        if (System.IO.Directory.Exists(newFullPath) && !data.Confirm)
                        {
                            return Json("conflict");
                        }
                        else
                        {
                            oldFolder.CopyTo(newFullPath);
                            return Json("success");
                        }
                    }
                }
                else
                {
                    if (System.IO.File.Exists(newFullPath) && !data.Confirm)
                    {
                        return Json("conflict");
                    }
                    else
                    {
                        System.IO.File.Copy(oldPath, newFullPath);
                        return Json("success");
                    }
                }
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        public ActionResult FileDelete(FilePostData data)
        {
            var repo = _GetRepo(data);
            if (repo == null)
            {
                return new HttpNotFoundResult();
            }
            try
            {
                // TODO: in the future manage issues with versions in this repo
                var path = repo.GetFullPath(data.Path, data.Name);

                var isFolder = false;

                if (!System.IO.File.Exists(path))
                {
                    if (!System.IO.Directory.Exists(path))
                    {
                        return new HttpNotFoundResult();
                    }
                    isFolder = true;
                }

                if (isFolder)
                {
                    System.IO.Directory.Delete(path, true);
                }
                else
                {
                    System.IO.File.Delete(path);
                }
                return Json("success");
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        public ActionResult FileUpload(FilePostData data)
        {
            var repo = _GetRepo(data);
            if (repo == null)
            {
                return new HttpNotFoundResult();
            }
            try
            {
                // TODO: in the future manage issues with versions in this repo
                var fullPath = repo.GetFullPath(data.Path, data.Name);

                if (System.IO.File.Exists(fullPath) && !data.Confirm)
                {
                    return Json("conflict");
                }
                else
                {
                    using (var stream = System.IO.File.OpenWrite(fullPath))
                    {
                        data.File.InputStream.CopyTo(stream);
                    }
                    return Json("success");
                }
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        public ActionResult DownloadProject(IDEPostData data)
        {
            var repo = _GetRepo(data);
            if (repo == null)
            {
                return new HttpNotFoundResult();
            }
            try
            {
                var name = "project.zip";
                //var zip = new Ionic.Zip.ZipFile();
                //zip.AddDirectory(repo.FilePath);
                //var ents = zip.SelectEntries(".git");
                //zip.RemoveEntries(ents);
                //var str = new System.IO.MemoryStream();
                //zip.Save(str);
                //str.Flush();
                //str.Seek(0, 0);
                //zip.Dispose();
                var str = repo.Folder.GetAsZipStream();
                var file = File(str, "application/zip", name);
                return file;
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        public class UrlPostData : IDEPostData
        {
            public string Path { get; set; } = "";
            public string Url { get; set; } = "";
        }

        [HttpPost]
        public ActionResult FileSaveUrl(UrlPostData data)
        {
            var repo = _GetRepo(data);
            if (repo == null)
            {
                return new HttpNotFoundResult();
            }
            try
            {
                // TODO: in the future manage issues with versions in this repo
                var path = repo.GetFullPath(data.Path);

                WebClient wc = new WebClient();
                wc.DownloadFile(data.Url, path);
                return Json("success");
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

    }
}
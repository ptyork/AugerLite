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

                if (User?.GetName() != data.UserName)
                {
                    return null;
                }

                repo = (T)Activator.CreateInstance(typeof(T), data.CourseId, data.UserName, data.RepositoryId, true);
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
            }
            return repo;
        }

        [HttpPost]
        public ActionResult GetFolder(IDEPostData data)
        {
            var repo = _GetRepo(data);
            if (repo == null)
            {
                return new HttpNotFoundResult();
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

            if (User?.GetName() != data.UserName)
            {
                return new HttpUnauthorizedResult();
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
                        RepositoryId = playground.PlaygroundId,
                        Name = playground.Name
                    });
                }

                var assignments = _db.StudentAssignments
                    .Include(sa => sa.Assignment.Course)
                    .Where(sa => sa.Enrollment.UserName == data.UserName)
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
            if (data.SourceType == ImportableProjects.Types.PLAYGROUND)
            {
                sourceRepo = PlaygroundRepository.Get(data.CourseId, data.UserName, data.SourceRepositoryId);
            }
            else if (data.SourceType == ImportableProjects.Types.WORKSPACE)
            {
                sourceRepo = WorkRepository.Get(data.CourseId, data.UserName, data.SourceRepositoryId);
            }
            else if (data.SourceType == ImportableProjects.Types.SUBMISSION)
            {
                sourceRepo = SubmissionRepository.Get(data.CourseId, data.UserName, data.SourceRepositoryId);
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
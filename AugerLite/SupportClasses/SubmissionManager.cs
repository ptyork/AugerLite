using Auger.DAL;
using Auger.Models;
using Auger.Models.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Auger
{
    public class SubmissionManager
    {
        private static String[] ZIP_MIMES = {
            "application/octet-stream",
            "binary/octet-stream",
            "multipart/x-zip",
            "application/zip",
            "application/zip-compressed",
            "application/x-zip-compressed"
        };

        public static StudentSubmission Submit(SubmissionModel model)
        {
            StudentSubmission submission = new StudentSubmission();

            if (String.IsNullOrWhiteSpace(model.Url) && model.ZipFile == null)
            {
                submission.Exception = "You must submit either a URL or a Zip file.";
                return submission;
            }
            if (!String.IsNullOrWhiteSpace(model.Url) && !NetHelper.TestUrl(model.Url))
            {
                submission.Exception = "Unable to load the submitted URL. Insure that the site is publicly accessible and that the URL begins either with http:// or https://.";
                return submission;
            }
            if (model.ZipFile != null)
            {
                if (!ZIP_MIMES.Contains(model.ZipFile.ContentType))
                {
                    Elmah.ErrorSignal.FromCurrentContext().Raise(new Exception($"WARNING: invalid zip file MIME: {model.ZipFile.ContentType}"));
                    submission.Exception = "You must submit a zip file.";
                    return submission;
                }
            }

            ApplicationUser user = null;
            if (string.IsNullOrWhiteSpace(model.UserName))
            {
                user = ApplicationUser.Current;
            }
            else
            {
                user = ApplicationUser.FromUserName(model.UserName);
            }

            if (user == null)
            {
                throw new AccessViolationException("Unable to retrieve user id");
            }

            using (var db = new AugerContext())
            {

                var studentAssignment = db.StudentAssignments
                    .Include(sa => sa.Assignment)
                    .Include(sa => sa.Enrollment.User)
                    .FirstOrDefault(sa => sa.AssignmentId == model.AssignmentId && sa.Enrollment.UserName == user.UserName);

                var repo = SubmissionRepository.Get(studentAssignment);

                try
                {
                    string commitId = null;
                    if (model.ZipFile != null)
                    {
                        commitId = repo.CommitAssignmentFromZip(model.ZipFile.InputStream);
                    }
                    else
                    {
                        commitId = repo.CommitAssignmentFromUrl(model.Url);
                    }
                    if (commitId != null)
                    {
                        submission.StudentAssignment = studentAssignment;
                        submission.StudentAssignment.AssignmentId = model.AssignmentId;
                        submission.CommitId = commitId;
                        submission.Succeeded = true;

                        db.StudentSubmissions.Add(submission);
                        studentAssignment.HasSubmission = true;

                        db.SaveChanges();
                    }
                    else
                    {
                        submission.Exception = "There were no changes detected. No new submission has been saved.";
                    }
                }
                catch (Exception ex)
                {
                    submission.Exception = ex.Message;
                    Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                }
            }

            return submission;
        }

        public static SubmissionFolder GetFolder(StudentSubmission submission)
        {
            var repo = SubmissionRepository.Get(submission.StudentAssignment);
            SubmissionFolder folder = new SubmissionFolder("_root", repo.FileUri);
            _FillFolder(repo.Directory, folder);
            return folder;
        }

        public static SubmissionFolder GetFolder(TempDir dir)
        {
            SubmissionFolder folder = new SubmissionFolder("_root", dir.FileUri);
            _FillFolder(dir.Directory, folder);
            return folder;
        }

        private static void _FillFolder(DirectoryInfo directory, SubmissionFolder folder)
        {
            foreach (var dirinfo in directory.GetDirectories())
            {
                if ((dirinfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                {
                    continue;
                }
                var subFolder = new SubmissionFolder(dirinfo.Name, new Uri(folder.Uri, dirinfo.Name));
                _FillFolder(dirinfo, subFolder);
                folder.Folders.Add(subFolder);
            }

            foreach (var fileinfo in directory.GetFiles())
            {
                if ((fileinfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                {
                    continue;
                }
                var file = new SubmissionFile(fileinfo.Name);
                folder.Files.Add(file);
            }

            var defaultfile = (from f in folder.Files
                               where f.Name.ToLowerInvariant().StartsWith("index.") || f.Name.ToLowerInvariant().StartsWith("default.")
                               where f.Type == FileType.HTML
                               select f).FirstOrDefault();

            if (defaultfile != null)
            {
                folder.Files.Remove(defaultfile);
                folder.Files.Insert(0, defaultfile);
            }
                              
        }
    }
}

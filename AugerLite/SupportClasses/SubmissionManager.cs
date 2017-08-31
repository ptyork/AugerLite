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

        public static StudentSubmission Submit(Repository workRepository)
        {
            StudentSubmission submission = new StudentSubmission();
            try
            {
                using (var db = new AugerContext())
                {
                    var studentAssignment = db.StudentAssignments
                        .Include(sa => sa.Assignment)
                        .Include(sa => sa.Enrollment.User)
                        .FirstOrDefault(sa => sa.AssignmentId == workRepository.RepositoryId && sa.Enrollment.UserName == workRepository.UserName);

                    if (studentAssignment == null)
                    {
                        var ex = new InvalidOperationException("Unable to retrieve the student assignment from the given repository.");
                        submission.Exception = ex.Message;
                        Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                    }
                    else
                    {
                        var repo = SubmissionRepository.Get(studentAssignment);

                        string commitId = repo.CommitFromRepository(workRepository);
                        if (commitId != null)
                        {
                            submission.StudentAssignment = studentAssignment;
                            //submission.StudentAssignment.AssignmentId = repo.RepositoryId;
                            submission.CommitId = commitId;
                            submission.Succeeded = true;

                            SubmissionTester.TestSubmission(submission);

                            db.StudentSubmissions.Add(submission);
                            studentAssignment.HasSubmission = true;

                            db.SaveChanges();
                        }
                        else
                        {
                            submission.Exception = "There were no changes detected. No new submission has been saved.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                submission.Exception = ex.Message;
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
            }

            return submission;
        }
    }
}

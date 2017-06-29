using Auger.Models.Data;

namespace Auger
{
    public class WorkRepository
    {
        private static string _basePath = null;
        public static void Init(string basePath)
        {
            _basePath = basePath;
            //if (!_basePath.StartsWith("\\\\?\\")) {
            //    _basePath = "\\\\?\\" + _basePath;
            //}
            System.IO.Directory.CreateDirectory(_basePath);
        }

        public static Repository Get(StudentAssignment studentAssignment)
        {
            var courseId = studentAssignment.Assignment.CourseId.ToString();
            var userId = studentAssignment.Enrollment.UserName.Trim().ToLowerInvariant();
            var assignmentId = studentAssignment.AssignmentId.GetValueOrDefault().ToString();
            return new Repository(_basePath, courseId, userId, assignmentId, true);
        }

        public static Repository Get(string courseId, string userId, string assignmentId)
        {
            return new Repository(_basePath, courseId, userId, assignmentId);
        }

    }
}

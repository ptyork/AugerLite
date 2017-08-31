using Auger.Models.Data;

namespace Auger
{
    public class SubmissionRepository : Repository
    {
        private static string _basePath;

        public static void Init(string basePath)
        {
            _basePath = basePath;
            System.IO.Directory.CreateDirectory(_basePath);
        }

        public static bool Exists(int courseId, string userName, int repositoryId)
        {
            return System.IO.Directory.Exists($"{_basePath}\\{courseId}\\{userName.Trim().ToLowerInvariant()}\\{repositoryId}");
        }

        public static SubmissionRepository Get(int courseId, string userName, int repositoryId, bool create = false)
        {
            return new SubmissionRepository(courseId, userName, repositoryId, create);
        }

        public static SubmissionRepository Get(StudentAssignment studentAssignment)
        {
            return Get(
                studentAssignment.Assignment.CourseId,
                studentAssignment.Enrollment.UserName.Trim().ToLowerInvariant(),
                studentAssignment.AssignmentId.GetValueOrDefault(),
                true
                );
        }

        public override string BasePath
        {
            get { return _basePath; }
        }

        public SubmissionRepository(int courseId, string userName, int repositoryId, bool create = false)
            : base(courseId, userName, repositoryId, create)
        {
        }

    }
}

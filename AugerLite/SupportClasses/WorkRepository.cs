using Auger.Models.Data;

namespace Auger
{
    public class WorkRepository : Repository
    {
        private static string _basePath;

        public static void Init(string basePath)
        {
            _basePath = basePath;
            System.IO.Directory.CreateDirectory(_basePath);
        }

        public static bool Exists(int courseId, string userName, int repositoryId)
        {
            var path = $"{_basePath}\\{courseId}\\{userName.Trim().ToLowerInvariant()}\\{repositoryId}";
            var exists = System.IO.Directory.Exists(path);
            if (exists)
            {
                var folder = System.IO.Directory.CreateDirectory(path);
                exists = folder.GetFiles().Length > 0;
            }
            return exists;
        }

        public static WorkRepository Get(int courseId, string userName, int repositoryId, bool create = false)
        {
            return new WorkRepository(courseId, userName, repositoryId, create);
        }

        public static WorkRepository Get(StudentAssignment studentAssignment)
        {
            return Get(
                studentAssignment.Assignment.CourseId,
                studentAssignment.Enrollment.UserName,
                studentAssignment.AssignmentId.GetValueOrDefault(),
                true
                );
        }

        public override string BasePath
        {
            get { return _basePath; }
        }

        public WorkRepository(int courseId, string userName, int repositoryId, bool create = false)
            : base(courseId, userName, repositoryId, create)
        {
        }

    }
}

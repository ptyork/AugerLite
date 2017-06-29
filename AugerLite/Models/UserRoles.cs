namespace Auger.Models
{
    public static class UserRoles
    {
        // LIS
        public const string LearnerRole = "Learner";
        public const string InstructorRole = "Instructor";
        public const string AdministratorRole = "Administrator";
        public const string TeachingAssistantRole = "TeachingAssistant";
        public const string ContentDeveloperRole = "ContentDeveloper";
        public const string MentorRole = "Mentor";

        // INTERNAL SYSTEM
        public const string SuperUserRole = "SuperUser";

        private static string[] _allRoles;
        private static string[] _allRolesLower;

        static UserRoles()
        {
            _allRoles = new string[]
                {
                    LearnerRole,
                    InstructorRole,
                    AdministratorRole,
                    TeachingAssistantRole,
                    ContentDeveloperRole,
                    MentorRole
                };
            _allRolesLower = new string[]
                {
                    LearnerRole.ToLowerInvariant(),
                    InstructorRole.ToLowerInvariant(),
                    AdministratorRole.ToLowerInvariant(),
                    TeachingAssistantRole.ToLowerInvariant(),
                    ContentDeveloperRole.ToLowerInvariant(),
                    MentorRole.ToLowerInvariant()
                };
        }

        public static string[] AllRoles
        {
            get { return _allRoles; }
        }

        public static string[] AllRolesLower
        {
            get { return _allRolesLower; }
        }
    }
}
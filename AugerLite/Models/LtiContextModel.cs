using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auger.Models
{
    public class LtiContextModel
    {
        public string LtiContextId { get; set; }
        public string CourseTitle { get; set; }
        public string CourseLabel { get; set; }
        public bool IsCourseLinked { get; set; } = false;
        public string[] Roles { get; set; }
        public string LtiResourceLinkId { get; set; }
        public string AssignmentName { get; set; }
        public bool IsAssignmentLinked { get; set; } = false;

        public bool IsInstructor
        {
            get
            {
                return Roles.Contains(UserRoles.InstructorRole);
            }
        }

        public bool IsLearner
        {
            get
            {
                return Roles.Contains(UserRoles.LearnerRole);
            }
        }

        public bool IsAdministrator
        {
            get
            {
                return Roles.Contains(UserRoles.AdministratorRole);
            }
        }

        public bool IsMentor
        {
            get
            {
                return Roles.Contains(UserRoles.MentorRole);
            }
        }

        public bool IsSuperUser
        {
            get
            {
                return Roles.Contains(UserRoles.SuperUserRole);
            }
        }

        public bool IsTeachingAssistant
        {
            get
            {
                return Roles.Contains(UserRoles.TeachingAssistantRole);
            }
        }
    }
}

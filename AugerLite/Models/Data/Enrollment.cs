using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace Auger.Models.Data
{
    public class Enrollment : EntityBase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EnrollmentId { get; set; }

        public int CourseId { get; set; }
        public virtual Course Course { get; set; }

        public string UserId { get; set; }
        private ApplicationUser _user;
        [ForeignKey("UserId")]
        public virtual ApplicationUser User
        {
            get { return _user; }
            set { _user = value; UserName = _user?.UserName; }
        }

        [MaxLength(128)]
        public string UserName { get; set; }

        private HashSet<string> _roles = new HashSet<string>();
        [NotMapped]
        public HashSet<string> Roles
        {
            get { return _roles; }
        }

        [MaxLength(128)]
        public string AllRoles
        {
            get
            {
                return string.Join(",", _roles);
            }
            set
            {
                var possibleRoles = value.ToLowerInvariant().Split(',');
                foreach (var possibleRole in possibleRoles)
                {
                    var role = possibleRole.Trim();
                    if (UserRoles.IsRole(role))
                    {
                        _roles.Add(role);
                    }
                }
            }
        }
        
        public bool IsActive { get; set; }
        
        public virtual ICollection<StudentAssignment> StudentAssignments { get; set; }

        public bool IsInRole(string role)
        {
            return _roles.ContainsIgnoreCase(role);
        }

    }
}

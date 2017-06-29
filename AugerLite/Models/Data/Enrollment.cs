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

        private List<string> _roles = new List<string>();
        private string _allRoles = null;
        [MaxLength(128)]
        public string AllRoles
        {
            get
            {
                return _allRoles;
            }
            set
            {
                var possibleRoles = value.Split(',');
                foreach (var possibleRole in possibleRoles)
                {
                    var role = possibleRole.ToLowerInvariant().Trim();
                    if (UserRoles.AllRolesLower.Contains(role))
                    {
                        _roles.Add(role);
                    }
                }
                _allRoles = string.Join(",", _roles);
            }
        }
        
        public bool IsActive { get; set; }
        
        public virtual ICollection<StudentAssignment> StudentAssignments { get; set; }

        [NotMapped]
        public IEnumerable<string> Roles
        {
            get { return _roles; }
        }

        public bool IsInRole(string role)
        {
            return _roles.Contains(role?.ToLowerInvariant());
        }

    }
}

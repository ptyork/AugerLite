using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using System.Web.Script.Serialization;

namespace Auger.Models.Data
{
    public class Course : EntityBase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CourseId { get; set; }

        [MaxLength(255)]
        public string CourseTitle { get; set; }

        [MaxLength(255)]
        public string CourseLabel { get; set; }

        [Index]
        [MaxLength(50)]
        public string LtiContextId { get; set; }

        public bool IsActive { get; set; }

        [JsonIgnore]
        [ScriptIgnore]
        public virtual ICollection<Assignment> Assignments { get; set; }

        [JsonIgnore]
        [ScriptIgnore]
        public virtual ICollection<Enrollment> Enrollments { get; set; }


        [NotMapped]
        public string FullName
        {
            get
            {
                return string.IsNullOrWhiteSpace(CourseLabel) ?
                    CourseTitle :
                    CourseTitle + " (" + CourseLabel + ")";
            }
        }

        [NotMapped]
        public string ShortName
        {
            get
            {
                return string.IsNullOrWhiteSpace(CourseLabel) ?
                    CourseTitle :
                    CourseLabel;
            }
        }

    }
}

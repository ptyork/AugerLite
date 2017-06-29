using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Auger.Models.Data
{
    public class StudentAssignment : EntityBase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int StudentAssignmentId { get; set; }

        // make nullable to remove multiple-path cascade
        public int? AssignmentId { get; set; }
        [ScriptIgnore]
        [JsonIgnore]
        public Assignment Assignment { get; set; }

        public int EnrollmentId { get; set; }
        [ScriptIgnore]
        [JsonIgnore]
        public Enrollment Enrollment { get; set; }

        public bool HasSubmission { get; set; }

        public List<StudentSubmission> Submissions { get; set; }

    }
}

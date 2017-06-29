using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web.Script.Serialization;

namespace Auger.Models.Data
{
    public class Assignment : EntityBase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AssignmentId { get; set; }

        public int CourseId { get; set; }

        [JsonIgnore]
        [ScriptIgnore]
        public virtual Course Course { get; set; }

        [MaxLength(255)]
        public string AssignmentName { get; set; }
        public DateTime? DueDate { get; set; }

        [Index]
        [MaxLength(50)]
        public string LtiResourceLinkId { get; set; }

        [JsonIgnore]
        [ScriptIgnore]
        public virtual ICollection<StudentAssignment> StudentAssignments { get; set; }

        [JsonIgnore]
        [ScriptIgnore]
        public virtual ICollection<Script> AllScripts { get; set; }

        [JsonIgnore]
        [ScriptIgnore]
        public IEnumerable<Script> CommonScripts
        {
            get
            {
                return AllScripts?.Where(s => !s.PageId.HasValue);
            }
        }

        [JsonIgnore]
        [ScriptIgnore]
        public IEnumerable<Script> CommonScriptsPreGrade
        {
            get
            {
                return CommonScripts?.Where(s => s.IsPreGrade);
            }
        }

        public virtual ICollection<Page> Pages { get; set; }
    }
}

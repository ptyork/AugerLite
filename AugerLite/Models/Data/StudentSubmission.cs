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
    public class StudentSubmission : EntityBase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int StudentSubmissionId { get; set; }

        public int StudentAssignmentId { get; set; }
        [ScriptIgnore]
        [JsonIgnore]
        public StudentAssignment StudentAssignment { get; set; }

        [MaxLength(128)]
        public string CommitId { get; set; }

        [MaxLength(255)]
        public string SubmissionName
        {
            get
            {
                var time = DateCreated;

                var name = time.ToShortDateString() + " " + time.ToShortTimeString();

                if (StudentAssignment == null || StudentAssignment.Assignment == null || !StudentAssignment.Assignment.DueDate.HasValue)
                {
                    return name;
                }

                var late = time > StudentAssignment.Assignment.DueDate;
                return late ? name + " (LATE)" : name;
            }
            set { }
        }
        public bool Succeeded { get; set; }
        public string Exception { get; set; }

        [ScriptIgnore]
        [JsonIgnore]
        public string PreSubmissionResultsJson
        {
            get
            {
                if (PreSubmissionResults == null)
                {
                    return null;
                }
                string json = JsonConvert.SerializeObject(PreSubmissionResults);
                return json;
            }
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    var results = JsonConvert.DeserializeObject<TestResults>(value);
                    PreSubmissionResults = results;
                }
            }
        }

        [ScriptIgnore]
        [JsonIgnore]
        public string FullResultsJson
        {
            get
            {
                if (FullResults == null)
                {
                    return null;
                }
                string json = JsonConvert.SerializeObject(FullResults);
                return json;
            }
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    var results = JsonConvert.DeserializeObject<TestResults>(value);
                    FullResults = results;
                }
            }
        }


        [NotMapped]
        public TestResults PreSubmissionResults { get; set; }

        [NotMapped]
        public TestResults FullResults { get; set; }

        [NotMapped]
        [ScriptIgnore]
        [JsonIgnore]
        public string UserName
        {
            get
            {
                return StudentAssignment?.Enrollment?.UserName;
            }
        }
    }
}

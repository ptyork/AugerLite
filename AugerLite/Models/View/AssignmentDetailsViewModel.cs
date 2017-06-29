using Auger.Models.Data;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auger.Models.View
{
    public class AssignmentDetailsViewModel
    {
        public Assignment Assignment { get; set; } = null;
        public List<StudentSubmission> Submissions { get; set; } = new List<StudentSubmission>();
        public SubmissionViewModel SelectedSubmission { get; set; } = null;
    }
}

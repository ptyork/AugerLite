using Auger.Models.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auger.Models.View
{
    public class AssignmentIndexViewModel
    {
        public Course Course { get; set; } = null;
        public List<Assignment> UnsubmittedAssignments { get; set; } = new List<Assignment>();
        public List<StudentSubmission> SubmittedAssignments { get; set; } = new List<StudentSubmission>();
    }
}

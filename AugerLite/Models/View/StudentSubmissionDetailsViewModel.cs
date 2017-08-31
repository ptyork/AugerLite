using Auger.Models.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auger.Models.View
{
    public class StudentAssignmentDetailsViewModel
    {
        public StudentAssignment StudentAssignment { get; set; } = null;
        public IEnumerable<StudentAssignment> AllAssignments { get; set; } = new List<StudentAssignment>();
    }
}

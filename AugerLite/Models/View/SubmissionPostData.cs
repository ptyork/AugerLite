using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auger.Models.View
{
    public class SubmissionPostData
    {
        public string UserId { get; set; }
        public int CourseId { get; set; }
        public int AssignmentId { get; set; }
        public int SubmissionId { get; set; }
    }
}

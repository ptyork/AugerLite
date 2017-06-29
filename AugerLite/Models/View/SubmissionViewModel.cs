using Auger.Models.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Auger.Models.View
{
    public class SubmissionViewModel
    {
        public StudentSubmission Submission { get; set; } = null;
        public SubmissionFolder Folder { get; set; }
    }
}

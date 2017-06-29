using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Auger.Models
{
    public class SubmissionModel
    {
        public string UserName { get; set; } // if null, use current user
        public int AssignmentId { get; set; }
        public string Url { get; set; } // null if ZipFile submitted
        public HttpPostedFileBase ZipFile { get; set; }
    }
}

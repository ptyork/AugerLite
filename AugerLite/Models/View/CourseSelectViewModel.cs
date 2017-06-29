using Auger.Models.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auger.Models.View
{
    public class CourseSelectViewModel
    {
        public IEnumerable<Course> Courses { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auger.Models
{
    public class DOMTestStats
    {
        public int Tests { get; set; } = 0;
        public int Passes { get; set; } = 0;
        public int Pending { get; set; } = 0;
        public int Failures { get; set; } = 0;
        //public DateTime Start { get; set; } = DateTime.MinValue;
        //public DateTime End { get; set; } = DateTime.MinValue;
        public int? Duration { get; set; } = 0;
    }
}

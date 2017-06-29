using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auger.Models
{
    public class DOMTestError
    {
        public string Message { get; set; } = "";
        public int LineNumber { get; set; } = 0;
        public int ColumnNumber { get; set; } = 0;
    }
}

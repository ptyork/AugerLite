using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auger.Models
{
    public class DOMTest
    {
        public string Title { get; set; } = "";
        public string FullTitle { get; set; } = "";
        public int? Duration { get; set; } = 0;
        public string Page { get; set; } = "";
        public List<string> Parents { get; set; } = new List<string>();
        public bool Passed { get; set; } = false;
        public DOMTestError Error { get; set; } = null;

        private string _id = null;
        public string Id
        {
            get
            {
                if (_id == null && this.Parents.Count > 0)
                {
                    var pars = this.Parents.ToList();
                    pars.Reverse();
                    _id = string.Join(".", pars);
                }
                return _id;
            }
        }

    }
}

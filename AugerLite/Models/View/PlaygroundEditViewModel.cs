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
    public class PlaygroundEditViewModel
    {
        public ApplicationUser User { get; set; } = null;
        public Course Course { get; set; } = null;
        public Playground Playground { get; set; } = null;
    }
}

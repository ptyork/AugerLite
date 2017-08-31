using Auger.Models.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auger.Models.View
{
    public class PlaygroundIndexViewModel
    {
        public Course Course { get; set; } = null;
        public List<Playground> Playgrounds { get; set; } = new List<Playground>();
    }
}

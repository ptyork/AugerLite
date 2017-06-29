using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace Auger.Models.Data
{
    public abstract class EntityBase
    {
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
    }
}

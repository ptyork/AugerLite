using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace Auger.Models.Data
{
    public class Script : EntityBase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ScriptId { get; set; }

        public int? AssignmentId { get; set; }
        public virtual Assignment Assignment { get; set; }

        public int? PageId { get; set; }
        public virtual Page Page { get; set; }


        [MaxLength(255)]
        public string ScriptName { get; set; }

        public string ScriptText { get; set; }

        public bool IsPreGrade { get; set; }

        [MaxLength(50)]
        public string DeviceId { get; set; }
        public Device Device
        {
            get
            {
                return Device.Parse(DeviceId);
            }
        }
    }
}

using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web.Script.Serialization;

namespace Auger.Models.Data
{
    public class Page : EntityBase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PageId { get; set; }

        public int AssignmentId { get; set; }

        [JsonIgnore]
        [ScriptIgnore]
        public virtual Assignment Assignment { get; set; }

        [MaxLength(255)]
        public string PageName { get; set; }

        [JsonIgnore]
        [ScriptIgnore]
        public virtual ICollection<Script> AllScripts { get; set; }

        [JsonIgnore]
        [ScriptIgnore]
        public IEnumerable<Script> Scripts
        {
            get
            {
                return AllScripts.AsEnumerable();
            }
        }

        [JsonIgnore]
        [ScriptIgnore]
        public IEnumerable<Script> ScriptsPreGrade
        {
            get
            {
                return AllScripts.Where(s => s.IsPreGrade);
            }
        }

    }
}

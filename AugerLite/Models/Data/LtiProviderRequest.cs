using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Auger.Models.Data
{
    public class LtiProviderRequest : EntityBase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LtiProviderRequestId { get; set; }

        public string LtiRequest { get; set; }
        public DateTime Received { get; set; }
    }
}

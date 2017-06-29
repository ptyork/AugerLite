using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Auger.Models.Data
{
    public class LtiOutcome : EntityBase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LtiOutcomeId { get; set; }

        public int LtiConsumerId { get; set; }
        public LtiConsumer LtiConsumer { get; set; }

        [MaxLength(255)]
        public string ContextTitle { get; set; }
        [MaxLength(255)]
        public string LisResultSourcedId { get; set; }
        [MaxLength(255)]
        public string ServiceUrl { get; set; }
    }
}
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Auger.Models.Data
{
    public class LtiConsumer : EntityBase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LtiConsumerId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; }

        [Required]
        [MaxLength(128)]
        public string Key { get; set; }
        
        [Required]
        [MaxLength(128)]
        public string Secret { get; set; }
    }
}
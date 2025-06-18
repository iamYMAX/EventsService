using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventsService.Models
{
    public class Cart
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ClientId { get; set; } // Links to the Client's Id
        [ForeignKey("ClientId")]
        public virtual Client? Client { get; set; } // Navigation property to the Client

        public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;

        public virtual ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    }
}

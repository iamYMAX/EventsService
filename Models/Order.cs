using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventsService.Models
{
    public enum OrderStatus
    {
        New,
        InProgress,
        Completed,
        Cancelled
    }

    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ClientId { get; set; }
        [ForeignKey("ClientId")]
        public virtual Client? Client { get; set; }

        public int? SalesRepresentativeId { get; set; } // Nullable if not always assigned immediately
        [ForeignKey("SalesRepresentativeId")]
        public virtual User? SalesRepresentative { get; set; }

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [DataType(DataType.DateTime)]
        [Display(Name = "Планируемая дата/время услуги")]
        public DateTime? ScheduledServiceDateTime { get; set; } // New property

        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.New;

        public virtual ICollection<OrderItem>? OrderItems { get; set; }
        public virtual ICollection<Checklist>? Checklists { get; set; }
    }
}

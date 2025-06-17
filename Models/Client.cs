using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventsService.Models
{
    public class Client
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(100)]
        public string? Organization { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        // Optional: Link to a User account if clients can log in
        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        public virtual ICollection<Order>? Orders { get; set; }
    }
}

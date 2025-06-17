using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // Required for StringLength and PersonalData

namespace EventsService.Models
{
    public class User : IdentityUser<int> // Use int for Id
    {
        [PersonalData] // Indicates this is personal data
        [StringLength(100, ErrorMessage = "Display Name cannot be longer than 100 characters.")]
        public string? DisplayName { get; set; }

        // Navigation property for Orders (SalesRepresentative)
        public virtual ICollection<Order>? SalesRepresentativeOrders { get; set; }

        // Navigation property for Client (if a user can also be a client)
        public virtual Client? Client { get; set; }
    }
}

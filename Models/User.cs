using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // Required for StringLength and PersonalData

namespace EventsService.Models
{
    public class User : IdentityUser<int> // Use int for Id
    {
        [PersonalData]
        [StringLength(50, ErrorMessage = "First Name cannot be longer than 50 characters.")]
        public string? FirstName { get; set; }

        [PersonalData]
        [StringLength(50, ErrorMessage = "Last Name cannot be longer than 50 characters.")]
        public string? LastName { get; set; }

        // Navigation property for Orders (SalesRepresentative)
        public virtual ICollection<Order>? SalesRepresentativeOrders { get; set; }

        // Navigation property for Client (if a user can also be a client)
        public virtual Client? Client { get; set; }
    }
}

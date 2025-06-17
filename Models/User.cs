using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace EventsService.Models
{
    public class User : IdentityUser<int> // Use int for Id
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Position { get; set; }

        // Navigation property for Orders (SalesRepresentative)
        public virtual ICollection<Order>? SalesRepresentativeOrders { get; set; }

        // Navigation property for Client (if a user can also be a client)
        public virtual Client? Client { get; set; }
    }
}

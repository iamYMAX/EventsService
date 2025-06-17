using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace EventsService.Models
{
    public class User : IdentityUser<int> // Use int for Id
    {
        // Navigation property for Orders (SalesRepresentative)
        public virtual ICollection<Order>? SalesRepresentativeOrders { get; set; }

        // Navigation property for Client (if a user can also be a client)
        public virtual Client? Client { get; set; }
    }
}

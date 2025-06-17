using Microsoft.AspNetCore.Identity;

namespace EventsService.Models
{
    public class Role : IdentityRole<int> // Use int for Id
    {
        // Additional properties for Role can be added here if needed
    }
}

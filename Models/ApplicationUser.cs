using Microsoft.AspNetCore.Identity;

namespace Houdini.Models
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public int? StoreID { get; set; }
        public Store Store { get; set; }
    }
}

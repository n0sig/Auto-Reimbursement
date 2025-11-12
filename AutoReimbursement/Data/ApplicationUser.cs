using Microsoft.AspNetCore.Identity;

namespace AutoReimbursement.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    public bool IsAdministrator { get; set; }
}


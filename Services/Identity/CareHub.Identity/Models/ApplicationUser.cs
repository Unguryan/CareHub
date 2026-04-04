using Microsoft.AspNetCore.Identity;

namespace CareHub.Identity.Models;

public class ApplicationUser : IdentityUser<Guid>
{
    public Guid BranchId { get; set; }
}

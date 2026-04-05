using Microsoft.AspNetCore.Identity;

namespace CareHub.Identity.Models;

public class ApplicationUser : IdentityUser<Guid>
{
    public Guid BranchId { get; set; }

    public long? TelegramChatId { get; set; }

    public string? TelegramUsername { get; set; }

    public DateTime? TelegramLinkedAt { get; set; }
}

using Microsoft.AspNetCore.Identity;

namespace TurniaCore.Data.Entities.Platform.User
{

    public class QueueUserRole : IdentityRole<Guid>
    {
        public string? Description { get; set; } = null!;
    }
}

using Microsoft.AspNetCore.Identity;
using TurniaCore.Data.Entities.App.Queue;

namespace TurniaCore.Entities.Platform.User
{
    public class QueueUserProfile : IdentityUser<Guid>
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public bool Active { get; set; }
        public DateTime Created { get; set; }
        public DateTime CreatedBy { get; set; }
        public DateTime LastLoggedIn { get; set; }
        public DateTime? AccountActivatedAt { get; set; }
        public DateTime? AccountDeActivatedAt { get; set; }
        public Guid? AssignedCounterId { get; set; }


        #region Relations
        public Counter? AssignedCounter { get; set; } = null!;
        public List<Ticket>? TicketsCalledBy { get; set; } = null!;
        #endregion
    }
}

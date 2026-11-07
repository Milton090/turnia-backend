using TurniaCore.Data.Entities.App.Queue;
using TurniaCore.Entities.Platform.User;

namespace TurniaCore.Data.Dto.Counters
{
    public class CounterDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; } = null!;
        public bool IsActive { get; set; }


#region Relations
        public QueueUserProfile? LoggedInUser { get; set; }
        public List<Service> Services { get; set; } = null!;
        public List<Ticket> Tickets { get; set; } = null!;
        #endregion
    }
}

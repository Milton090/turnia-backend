using TurniaWeb.Areas.Queue.Dtos.Ticket;

namespace TurniaWeb.Areas.Queue.Dtos.Counter
{
    public class CounterInfoDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; } = null!;
        public bool IsActive { get; set; }

        public TicketInfoDto TicketInfo { get; set; } = null!;

        public User.UserInfoDto QueueUserProfile { get; set; } = null!;

    }
}

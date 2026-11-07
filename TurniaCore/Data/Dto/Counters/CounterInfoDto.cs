using TurniaCore.Data.Dto.Tickets;
using TurniaCore.Data.Dto.User;

namespace TurniaCore.Data.Dto.Counters
{
    public class CounterInfoDto
    {

        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; } = null!;
        public bool IsActive { get; set; }

        public TicketInfoDto? TicketInfo { get; set; } = null!;

        public UserInfoDto? QueueUserProfile { get; set; } = null!;


    }
}

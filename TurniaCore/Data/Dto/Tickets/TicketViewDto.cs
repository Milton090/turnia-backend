namespace TurniaCore.Data.Dto.Tickets
{
    public class TicketViewDto
    {
        public List<Entities.App.Queue.Ticket> CallingTickets { get; set; } = null!;
        public List<Entities.App.Queue.Ticket> AttendingTickets { get; set; } = null!;
    }
}

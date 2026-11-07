namespace TurniaWeb.Areas.Queue.Dtos.Ticket
{
    public class TicketInfoDto
    {
        public Guid Id { get; set; }
       
        public TicketStatusEnum TicketStatus { get; set; }
        public enum TicketStatusEnum
        {
            Unattended = 0,
            CallInProgress = 1,
            StartAttending = 2,
            FinishedAttending = 3,
            NoShow = 4,
            Cancelled = 5
        }
    }
}

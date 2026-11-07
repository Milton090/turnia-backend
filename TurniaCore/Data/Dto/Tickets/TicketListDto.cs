namespace TurniaCore.Data.Dto.Tickets
{
    public class TicketListDto
    {
        public List<TicketDto> TodaysTickets { get; set; } = null!;
        public List<TicketDto> UnAttendedTickets { get; set; } = null!;
        public List<TicketDto> UnAttendedTicketsPerCounter { get; set; } = null!;
    }
}

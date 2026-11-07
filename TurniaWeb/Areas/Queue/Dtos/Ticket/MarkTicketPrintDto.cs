namespace TurniaWeb.Areas.Queue.Dtos.Ticket
{
    public class MarkTicketPrintDto
    {
        public Guid Id { get; set; }
        public string DeviceKey { get; set; } = null!;
    }
}

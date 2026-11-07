namespace TurniaWeb.Areas.Queue.Dtos.Ticket
{
    public class AddTicketDto
    {
        public Guid DeviceId { get; set; }
        public string DeviceKey { get; set; } = null!;
        public Guid Service { get; set; }
    }
}

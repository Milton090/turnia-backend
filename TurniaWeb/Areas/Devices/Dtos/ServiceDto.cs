namespace TurniaWeb.Areas.Devices.Dtos
{
    public class ServiceDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string TicketCode { get; set; } = null!;
        public int SortIndex { get; set; }
        public bool IsActive { get; set; }

    }
}

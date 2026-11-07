namespace TurniaWeb.Areas.Devices.Dtos
{
    public class CounterDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; } = null!;
        public bool IsActive { get; set; }
        public List<ServiceDto> Services { get; set; } = null!;
    }
}

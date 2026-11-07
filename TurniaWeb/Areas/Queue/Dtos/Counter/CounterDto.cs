namespace TurniaWeb.Areas.Queue.Dtos.Counter
{
    public class CounterDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; } = null!;
        public bool IsActive { get; set; }
    }
}

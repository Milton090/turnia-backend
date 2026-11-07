namespace TurniaWeb.Areas.Queue.Dtos.Counter
{
    public class UpdateCounterDto : CounterDto
    {
        public List<Guid>? Services { get; set; } = null;
    }
}

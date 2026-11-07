namespace TurniaWeb.Areas.Queue.Dtos.Counter
{
    public class AddCounterDto : CounterDto
    {
        public List<Guid>? Services { get; set; } = null;
    }
}

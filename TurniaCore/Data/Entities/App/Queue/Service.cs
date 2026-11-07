using TurniaCore.Data.Entities.BaseEntities;

namespace TurniaCore.Data.Entities.App.Queue
{
    public class Service : BaseEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string TicketCode { get; set; } = null!;
        public int SortIndex { get; set; }
        
        public bool IsActive { get; set; }


#region Relations
        public List<Counter>? Counters { get; set; }
        public List<Ticket> Tickets { get; set; } = null!;
        #endregion

    }
}

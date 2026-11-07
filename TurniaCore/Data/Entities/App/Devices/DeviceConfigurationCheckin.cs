using TurniaCore.Data.Entities.BaseEntities;
using TurniaCore.Data.Entities.App.Queue;

namespace TurniaCore.Data.Entities.Platform.Devices
{
    public class DeviceConfigurationCheckin : BaseEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;

        public string? DeviceKey { get; set; } = null!;

        #region Relations
        public List<Ticket>? Tickets { get; set; } = null!;
        #endregion


}
}

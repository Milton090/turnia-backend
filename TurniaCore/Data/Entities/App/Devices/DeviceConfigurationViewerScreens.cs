using TurniaCore.Data.Entities.BaseEntities;

namespace TurniaCore.Data.Entities.Platform.Devices
{
    public class DeviceConfigurationViewerScreens : BaseEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? ScreenKey { get; set; } = null!;

        public int? ColumnNumber { get; set; }

    }
}

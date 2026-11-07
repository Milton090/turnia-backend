using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using TurniaCore.Data.Entities.Platform.Devices;

namespace TurniaCore.Data.Configuration.App.Devices
{
    public class DeviceConfigurationViewerScreenConfiguration : IEntityTypeConfiguration<DeviceConfigurationViewerScreens>
    {
        public void Configure(EntityTypeBuilder<DeviceConfigurationViewerScreens> builder)
        {
            builder.ToTable("CONFIGDeviceConfigurationViewerScreens");

            builder.HasIndex(p => p.ScreenKey).IsUnique();
        }
    }
}

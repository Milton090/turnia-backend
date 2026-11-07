using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using TurniaCore.Data.Entities.Platform.Devices;

namespace TurniaCore.Data.Configuration.App.Devices
{
    public class DeviceConfigurationCheckinConfiguration : IEntityTypeConfiguration<DeviceConfigurationCheckin>
    {
        public void Configure(EntityTypeBuilder<DeviceConfigurationCheckin> builder)
        {
            builder.ToTable("CONFIGDeviceConfigurationCheckin");

            builder.HasIndex(p => p.DeviceKey).IsUnique();
        }
    }
}

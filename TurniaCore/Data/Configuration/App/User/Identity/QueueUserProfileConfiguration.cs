using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using TurniaCore.Entities.Platform.User;

namespace TurniaCore.Data.Configuration.App.Queue
{
    public class QueueUserProfileConfiguration : IEntityTypeConfiguration<QueueUserProfile>
    {
        public void Configure(EntityTypeBuilder<QueueUserProfile> builder)
        {
            #region Indexes
            builder.HasIndex(x => x.Email);
            builder.HasIndex(x => x.UserName);
            builder.HasIndex(x => x.NormalizedUserName);
            builder.HasIndex(x => x.NormalizedEmail);
            builder.HasIndex(x => x.AssignedCounterId);
            builder.HasIndex(x => x.Active).IsUnique(false);
            #endregion
        }
    }
}

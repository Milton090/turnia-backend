using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using TurniaCore.Data.Entities.App.Queue;
using TurniaCore.Entities.Platform.User;

namespace TurniaCore.Data.Configuration.App.Queue
{
    public class CounterConfiguration : IEntityTypeConfiguration<Counter>
    {
        public void Configure(EntityTypeBuilder<Counter> builder)
        {
            builder.ToTable("Counters");

            builder.HasOne(p => p.LoggedInUser).WithOne(x => x.AssignedCounter)
                .HasForeignKey<QueueUserProfile>(p => p.AssignedCounterId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            #region Indexes
            builder.HasIndex(x => x.Name);
            builder.HasIndex(x => x.IsActive);
            builder.HasIndex(x => x.Created);
            #endregion

            #region Indexes on Ids
            builder.HasIndex(x => x.LoggedInUserId);
            #endregion
        }


    }
}
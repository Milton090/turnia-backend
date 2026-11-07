using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using TurniaCore.Data.Entities.App.Queue;

namespace TurniaCore.Data.Configuration.App.Queue
{
    public class ServiceConfiguration : IEntityTypeConfiguration<Service>
    {
        public void Configure(EntityTypeBuilder<Service> builder)
        {
            builder.ToTable("Services");

            builder.HasIndex(x => x.SortIndex);
            builder.HasIndex(x => x.IsActive);
        }
    }
}

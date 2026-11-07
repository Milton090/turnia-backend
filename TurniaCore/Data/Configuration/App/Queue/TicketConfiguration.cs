using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using TurniaCore.Data.Entities.App.Queue;

namespace TurniaCore.Data.Configuration.App.Queue
{
    public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
    {
        public void Configure(EntityTypeBuilder<Ticket> builder)
        {
            builder.ToTable("Tickets");

            #region Relations - Ticket
            builder.HasOne(x => x.Service)
                .WithMany(x => x.Tickets)
                .IsRequired()
                .HasForeignKey(x => x.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Device)
              .WithMany(x => x.Tickets)
              .IsRequired()
              .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Counter)
             .WithMany(x => x.Tickets)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.CalledBy)
                .WithMany(x => x.TicketsCalledBy)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            #endregion

            #region Indexes
            builder.HasIndex(x => x.Created);
            builder.HasIndex(x => x.TicketDate);
            builder.HasIndex(x => x.TicketNumberSequence);
            builder.HasIndex(x => x.TicketStatus);
            builder.HasIndex(x => x.TicketPriority);
            builder.HasIndex(x => x.TicketLatestCallDateTime);
            builder.HasIndex(x => x.TicketAttendingStartDateTime);
            #endregion

            #region Indexes on Ids
            builder.HasIndex(x => x.ServiceId);
            builder.HasIndex(x => x.CalledById);
            builder.HasIndex(x => x.CounterId);
            #endregion
        }
    }
}

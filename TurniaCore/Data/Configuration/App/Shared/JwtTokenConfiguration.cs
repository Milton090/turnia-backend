using TurniaCore.Data.Entities.App.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TurniaCore.Data.Configuration.App.Shared
{
    public class JwtTokenConfiguration :IEntityTypeConfiguration<JwtRefreshTokens>
    {
        public void Configure(EntityTypeBuilder<JwtRefreshTokens> builder)
        {
            builder.ToTable("JwtRefreshTokens");
            #region Indexes
            builder.HasIndex(b => b.RefreshToken).IsUnique();
            #endregion
        }
    }
}

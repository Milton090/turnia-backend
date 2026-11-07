using TurniaCore.Entities.Platform.User;
using System.ComponentModel.DataAnnotations;

namespace TurniaCore.Data.Entities.App.Shared
{
    public class JwtRefreshTokens
    {
        public Guid Id { get; set; }
        [Required]
        public DateTime Created { get; set; }
        [Required]
        public string RefreshToken { get; set; } = null!;

        public DateTime ExpireTime { get; set; }
        public bool Revoked { get; set; }
        public DateTime? RevokedDateTime { get; set; }
        public bool Exchanged { get; set; }
        public DateTime? ExchangedDateTime { get; set; }

        public string MetaUserAgent { get; set; } = null!;
        public string MetaFromIpAddress { get; set; } = null!;


#region Relations 

        public QueueUserProfile User { get; set; } = null!;
        public Guid UserId { get; set; }

        #endregion
    }
}

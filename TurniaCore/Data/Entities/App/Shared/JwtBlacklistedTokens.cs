using TurniaCore.Entities.Platform.User;

namespace TurniaCore.Data.Entities.App.Shared
{
    public class JwtBlacklistedTokens
    {
        public Guid Id { get; set; }
        public string TokenHash { get; set; } = null!;
        public DateTime TokenExpiry { get; set; }


        #region Relations 

        public QueueUserProfile User { get; set; } = null!;
        public Guid UserId { get; set; }

        #endregion

    }
}

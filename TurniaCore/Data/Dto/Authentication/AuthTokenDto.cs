

namespace TurniaCore.Data.Dto.Authentication
{
    public class AuthTokenDto
    {
        public string token_type { get; set; } = string.Empty;
        public string access_token { get; set; } = string.Empty;
        public DateTime expires { get; set; }
        public string refresh_token { get; set; } = string.Empty;
    }
}
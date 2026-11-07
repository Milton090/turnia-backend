using System.ComponentModel.DataAnnotations;


namespace TurniaCore.Data.Dto.Authentication
{
    public class RefreshTokenDto
    {
        [Required]
        public string access_token { get; set; } = string.Empty;
        [Required]
        public string refresh_token { get; set; } = string.Empty;
    }
}
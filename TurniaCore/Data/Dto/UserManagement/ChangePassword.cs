namespace TurniaCore.Data.Dto.UserManagement
{
    public class ChangePassword
    {
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

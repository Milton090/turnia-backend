namespace TurniaCore.Data.Dto.UserManagement
{
    public class ChangeUserPassword
    {
        public string UserName { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

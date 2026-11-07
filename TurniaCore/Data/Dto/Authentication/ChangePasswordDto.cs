namespace TurniaCore.Data.Dto.Authentication;

public class ChangePasswordDto
{
    public string NewPassword { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;
}
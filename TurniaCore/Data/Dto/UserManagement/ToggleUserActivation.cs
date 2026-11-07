namespace TurniaCore.Data.Dto.UserManagement
{
    public class ToggleUserActivation
    {
        public Guid UserId { get; set; }
        public bool Activate { get; set; }
    }
}

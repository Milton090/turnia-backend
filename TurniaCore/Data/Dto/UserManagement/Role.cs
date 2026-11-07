namespace TurniaCore.Data.Dto.UserManagement
{
    public class Role
    {
        public Guid UserId { get; set; }
        public List<string> RoleName { get; set; } = new List<string>();
    }
}

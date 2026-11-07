namespace TurniaCore.Data.Dto.UserManagement
{
    public class EditUser
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public Guid? AssignedCounterId { get; set; }
    }
}

namespace TurniaCore.Data.Dto.UserManagement
{
    public class CreateUser
    {
        public string UserName { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? Password { get; set; }
        public bool Active { get; set; }
        public string UserRole { get; set; } = string.Empty!;
        public Guid? AssignedCounterId { get; set; }
    }
}

namespace TurniaCore.Data.Dto.User
{
    public class UserInfoDto
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public bool Active { get; set; }
        public Guid? AssignedCounterId { get; set; }
    }
}
namespace TurniaCore.Data.Dto.User
{
    public class UserDetailDto : UserInfoDto
    {
        public Guid Id { get; set; }
        public List<string> Role { get; set; } = new List<string>();
        public string UserName { get; set; } = string.Empty;
    }
}

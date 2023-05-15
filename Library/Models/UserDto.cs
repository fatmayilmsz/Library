namespace Library.Models
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public byte Role { get; set; }
        public bool Approved { get; set; }
    }
}

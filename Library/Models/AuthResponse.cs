namespace Library.Models
{
    public class AuthResponse
    {
        public string Name { get; set; }
        public string LastName { get; set; }

        public string Email { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public int Role { get; set; }
        public string? Phone { get; set; }
        public string Token { get; set; }
        public bool Approved { get; set; }
    }
}

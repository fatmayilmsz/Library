namespace Library.Models
{
    public class User 
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public byte Role { get; set; }
        public ICollection<Book>? Books { get; set; }
        public bool Approved { get; set; }
    }
}

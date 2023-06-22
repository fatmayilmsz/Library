namespace Library.Models
{
    public class BookUser
    {
        public int Id { get; set; }
        public User User { get; set; }
        public Book Book { get; set; }
        public bool Availability { get; set; }
        public Queue<User> UserQueue { get; set; }
    }
}

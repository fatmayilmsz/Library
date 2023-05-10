namespace Library.Models
{
    public class Author
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? LastName { get; set; }
        public byte[]? Image { get; set; }
        public ICollection<Category>? Categories { get; set; }
        public ICollection<Book>? Books { get; set; }
    }
}

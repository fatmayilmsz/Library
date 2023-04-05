namespace Library.Models
{
    public class Book
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int AuthorId { get; set; }
        public string Author { get; set; }
        public string Publishing { get; set; }
        public int CategoryId { get; set; }
        public string Category { get; set; }
        public string? Summary { get; set; }
        public int ReadCount { get; set; }
        public byte[]? Image { get; set; }
    }
}
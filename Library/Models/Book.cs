namespace Library.Models
{
    public class Book
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public ICollection<Author>? Authors { get; set; }
        public ICollection<Publisher>? Publishers { get; set; }
        public ICollection<Category>? Categories { get; set; }
        public string? Summary { get; set; }
        public int? ReadCount { get; set; }
        public byte[]? Image { get; set; }
    }
}
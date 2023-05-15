namespace Library.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public ICollection<Author>? Authors { get; set; }
        public ICollection<Book>? Books { get; set; }
        public bool Approved { get; set; }
    }
}

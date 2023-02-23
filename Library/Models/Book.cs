namespace Library.Models
{
    public class Book
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Author { get; set; }
        public string Publishing { get; set; }
        public int Basım { get; set; }
        public int Category { get; set; }
        public byte[]? Image { get; set; }
    }
}

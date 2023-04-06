using Microsoft.AspNetCore.Identity;

namespace Library.Models
{
    public class Role : IdentityRole<Guid>
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}

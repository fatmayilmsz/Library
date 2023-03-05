using Microsoft.AspNetCore.Identity;

namespace Library.Models
{
    public class Role : IdentityRole<Guid>
    {

        public string NormalizedName { get; set; }
        public string ConcurrencyStamp { get; set; }
    }
}

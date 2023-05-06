using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace Library.Models
{
    public class LibraryContext : IdentityDbContext
    {
  

        public LibraryContext(DbContextOptions<LibraryContext> options) :base(options) { }
        public DbSet<Book> Books { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserDto> UserDtos { get; set; }
        public DbSet<Address> Address { get; set; }
        public DbSet<Author> Authors { get; set; }

      
    }
  

}

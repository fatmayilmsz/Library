using Library.Models;
using Microsoft.AspNetCore.Mvc;

namespace Library.Controllers
{
    [ApiController]
    [Route("authors")]
    public class AuthorController : ControllerBase
    {
        private readonly LibraryContext _context;
        public AuthorController(LibraryContext librarycontext)
        {
            _context = librarycontext;
        }
        [HttpGet]
        public IActionResult FindAuthors()
        {
            var authors = _context.Authors.ToList();
            return Ok(authors);
        }
    }
}

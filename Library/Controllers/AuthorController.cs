using Library.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        public async Task<IActionResult> FindAuthors()
        {
            return Ok(await _context.Authors.ToListAsync());
        }
    }
}

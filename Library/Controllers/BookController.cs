using Library.Models;
using Microsoft.AspNetCore.Mvc;

namespace Library.Controllers
{
    [ApiController]
    [Route("books")]
    public class BookController : ControllerBase
    {
        private readonly LibraryContext _context;
        public BookController(LibraryContext librarycontext)
        {
            _context = librarycontext;
        }
        [HttpGet]
        public IActionResult FindBooks()
        {
            var books = _context.Books.ToList();
            return Ok(books);
        }
    }
}

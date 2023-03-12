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
        [HttpPost]
        public IActionResult CreateBook(Book book)
        {
            if (book == null)
            {
                return BadRequest("Invalid data");
            }
            _context.Books.Add(new Book()
            {
                Name = book.Name,
                Author= book.Author,
                Publishing = book.Publishing,
                Category= book.Category,
                Summary= book.Summary,

            });
            _context.SaveChanges();
            return Ok();
        }
    }
}
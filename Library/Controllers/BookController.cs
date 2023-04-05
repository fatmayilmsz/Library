using Library.Models;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

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

        [HttpGet]
        [Route("/{category}/{id}")]
        public IActionResult GetBook(UInt32 category, UInt32 id)
        {
            try
            {
                Book book = _context.Books.Where(x => x.CategoryId == category && x.Id == id).First();
                return Ok(book);
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        [HttpPost]
        [Route("/update")]
        public IActionResult UpdateBook(Book book)
        {
            try
            {
                Book bookdb = _context.Books.Where(x => x.CategoryId == book.CategoryId && x.Id == book.Id).First();

                foreach (PropertyInfo prop in book.GetType().GetProperties())
                {
                    var propVal = prop.GetValue(book);
                    if (propVal != null && !propVal.Equals("") && !propVal.Equals(0))
                    {
                        if (bookdb.GetType().GetProperty(prop.Name) != null)
                        {
                            bookdb.GetType().GetProperty(prop.Name).SetValue(bookdb, propVal);
                        }
                    }
                }

                _context.Books.Update(bookdb);
                _context.SaveChanges();

                return Ok();
            }
            catch (Exception e)
            {
                return NotFound(e);
            }
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
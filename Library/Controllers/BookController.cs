using Library.Models;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using Microsoft.EntityFrameworkCore;

namespace Library.Controllers
{
    [ApiController]
    public class BookController : ControllerBase
    {
        private readonly LibraryContext _context;
        public BookController(LibraryContext librarycontext)
        {
            _context = librarycontext;
        }

        [HttpGet, Route("books")]
        public async Task<IActionResult> FindBooks()
        {
            return Ok(await _context.Books.ToListAsync());
        }

        [HttpGet, Route("books/{category}/{id}")]
        public async Task<IActionResult> GetBook(UInt32 category, UInt32 id)
        {
            try
            {
                return Ok(await _context.Books.Where(x => x.CategoryId == category && x.Id == id).FirstAsync());
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        [HttpDelete, Route("books/delete/{category}/{id}")]
        public async Task<IActionResult> DeleteBook(UInt32 category, UInt32 id)
        {
            try
            {
                _context.Remove(await _context.Books.Where(x => x.CategoryId == category && x.Id == id).FirstAsync());
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        [HttpPut, Route("books/update")]
        public async Task<IActionResult> UpdateBook(Book book)
        {
            try
            {
                Book bookdb = await _context.Books.Where(x => x.CategoryId == book.CategoryId && x.Id == book.Id).FirstAsync();

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
                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        [HttpPost, Route("books/add")]
        public async Task<IActionResult> CreateBook(Book book)
        {
            if (book != null)
            {
                if (book.Image != null)
                {
                    using (System.IO.MemoryStream ms = new MemoryStream(book.Image)) //Görselin kalitesi düşürülür
                    {
                        using (SixLabors.ImageSharp.Image image = Image.Load(ms))
                        {
                            image.Mutate(x => x.Resize(new ResizeOptions
                            {
                                Size = new Size(210, 150),
                                Mode = ResizeMode.Max,
                                Compand = true
                            }));
                            ms.SetLength(0);
                            await image.SaveAsync(ms, new JpegEncoder { Quality = 80 });
                            book.Image = ms.ToArray();
                        }
                    }
                }

                await _context.Books.AddAsync(new Book()
                {
                    Name = book.Name,
                    Author = book.Author,
                    Publishing = book.Publishing,
                    Category = book.Category,
                    Summary = book.Summary,
                    Image = book.Image,
                });
                await _context.SaveChangesAsync();
                return Accepted();
            }
            return BadRequest("Invalid data");
        }
    }
}
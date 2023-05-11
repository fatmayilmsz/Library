using Library.Models;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using SixLabors.ImageSharp.Formats.Jpeg;
using Microsoft.EntityFrameworkCore;
using System.Collections;

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

        [HttpPost, Route("books/add")]
        public async Task<IActionResult> AddBook(Book book)
        {
            //bool check = book.Authors?.Count != 0 && book.Authors != null ? await _context.Authors.AnyAsync(a => book.Authors.Any(auth => auth == a)) : true;
            if (book != null && !await _context.Books.Include(b => b.Authors)
                .AnyAsync(b => b.Name == book.Name))
            {
                if (book.Image != null && book.Image.Length != 0)
                {
                    using (System.IO.MemoryStream ms = new MemoryStream(book.Image)) //Görselin kalitesi düşürülür
                    {
                        using (SixLabors.ImageSharp.Image image = Image.Load(ms))
                        {
                            image.Mutate(ipc => ipc.Resize(new ResizeOptions
                            {
                                Size = new Size(150, 210),
                                Mode = ResizeMode.Max,
                                Compand = true
                            }));
                            ms.SetLength(0);
                            await image.SaveAsync(ms, new JpegEncoder { Quality = 80 });
                            book.Image = ms.ToArray();
                        }
                    }
                }

                Author[] matchedAuthors = await _context.Authors
                    .Where(a => book.Authors!
                    .Select(author => author.Id)
                        .Contains(a.Id))
                    .ToArrayAsync();

                Publisher[] matchedPublishers = await _context.Publishers
                    .Where(p => book.Publishers!
                    .Select(publisher => publisher.Id)
                        .Contains(p.Id))
                    .ToArrayAsync();

                Category[] matchedCategories = await _context.Categories
                    .Where(c => book.Categories!
                    .Select(cat => cat.Id)
                        .Contains(c.Id))
                    .ToArrayAsync();

                await _context.Books.AddAsync(new Book()
                {
                    Name = book.Name,
                    Authors = matchedAuthors,
                    Publishers = matchedPublishers,
                    Categories = matchedCategories,
                    Summary = book.Summary,
                    Image = book.Image,
                });

                await _context.SaveChangesAsync();
                return Accepted();
            }
            return BadRequest("Invalid data");
        }

        [HttpGet, Route("books")]
        public async Task<IActionResult> FindBooks()
        {
            return Ok(await _context.Books.AsNoTracking()
                .ToArrayAsync());
        }

        [HttpGet, Route("books/{id}")]
        public async Task<IActionResult> GetBook( UInt32 id)
        {
            try
            {
                return Ok(await _context.Books
                    .Include(b => b.Categories)
                    .Include(b => b.Authors)
                    .Include(b => b.Publishers)
                    .AsNoTracking()
                    .Where(b => b.Id == id)
                    .Select(b => new
                    {
                        b.Id,
                        b.Name,
                        b.Summary,
                        b.Image,
                        Categories = b.Categories.Select(c => new
                        {
                            c.Id,
                            c.Name
                        }),
                        Authors = b.Authors.Select(a => new
                        {
                            a.Id,
                            a.Name,
                            a.LastName,
                            a.Image
                        }),
                        Publishers = b.Publishers.Select(p => new
                        { 
                            p.Id,
                            p.Name
                        })
                    })
                    .FirstAsync());
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
                Book bookdb = await _context.Books
                    .SingleAsync(b => b.Id == book.Id);
                string msg = "";

                foreach (PropertyInfo prop in book.GetType().GetProperties().ToArray())
                {
                    var propVal = prop.GetValue(book) ?? null;
                    if (propVal != null && !propVal.Equals("") && !propVal.Equals(0)
                        && (propVal as ICollection)?.Count != 0
                        && bookdb.GetType().GetProperty(prop.Name) != null)
                    {
                        if (prop.Name == "Name" || prop.Name == "Summary" || prop.Name == "Image")
                        {
                            bookdb.GetType()?.GetProperty(prop.Name)?.SetValue(bookdb, propVal);
                        }
                        else
                        {
                            switch (prop.Name)
                            {
                                case "Categories":
                                    if (book.Categories != null)
                                    {
                                        Category[] newCats = await _context.Categories
                                            .Where(c => book.Categories!
                                            .Select(c => c.Id).Contains(c.Id)).ToArrayAsync();
                                        if (newCats.Length != 0)
                                        {
                                            bookdb.Categories?.Clear();
                                            bookdb.Categories = newCats;
                                        }
                                        else
                                        {
                                            msg += "Category Update Failed - No Category Matches\n";
                                        }
                                    }
                                    break;
                                case "Authors":
                                    if (book.Authors != null)
                                    {
                                        Author[] newAuthors = await _context.Authors
                                            .Where(
                                                a => book.Authors!
                                                .Select(a => a.Id)
                                                .Contains(a.Id))
                                            .ToArrayAsync();
                                        if (newAuthors.Length != 0)
                                        {
                                            bookdb.Authors?.Clear();
                                            bookdb.Authors = newAuthors;
                                        }
                                        else
                                        {
                                            msg += "Author Update Failed - No Author Matches\n";
                                        }
                                    }
                                    break;
                                case "Publishers":
                                    if (book.Publishers != null)
                                    {
                                        Publisher[] newPublishers = await _context.Publishers
                                            .Where(
                                                p => book.Publishers!
                                                .Select(p => p.Id)
                                                .Contains(p.Id))
                                            .ToArrayAsync();
                                        if (newPublishers.Length != 0)
                                        {
                                            bookdb.Publishers?.Clear();
                                            bookdb.Publishers = newPublishers;
                                        }
                                        else
                                        {
                                            msg += "Publisher Update Failed - No Publisher Matches\n";
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }

                _context.Books.Update(bookdb);
                await _context.SaveChangesAsync();
                return Ok(new { message = msg });
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        [HttpDelete, Route("books/delete/{id}")]
        public async Task<IActionResult> DeleteBook(UInt32 id)
        {
            try
            {
                Book tempBook = await _context.Books
                    .Include(b => b.Categories)
                    .Include(b => b.Authors)
                    .Include(b => b.Publishers)
                    .Where(b => b.Id == id)
                    .FirstAsync();
                tempBook.Categories.Clear();
                tempBook.Authors.Clear();
                tempBook.Publishers.Clear();
                _context.Remove(tempBook);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception)
            {
                return NotFound();
            }
        }
    }
}
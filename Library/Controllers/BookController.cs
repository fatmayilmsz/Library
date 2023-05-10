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

        [HttpGet, Route("books")]
        public async Task<IActionResult> FindBooks()
        {
            return Ok(await _context.Books.AsNoTracking()
                .ToListAsync());
        }

        [HttpGet, Route("books/{category}/{id}")]
        public async Task<IActionResult> GetBook(UInt32 category, UInt32 id)
        {
            try
            {
                return Ok(await _context.Books
                    .Include(x => x.Categories)
                    .Include(x => x.Authors)
                    .AsNoTracking()
                    .Where(x => x.Categories.Any(y => y.Id == category) && x.Id == id)
                    .Select(x => new
                    {
                        x.Id,
                        x.Name,
                        x.Summary,
                        x.Image,
                        Categories = x.Categories.Select(x => new
                        {
                            x.Id,
                            x.Name
                        }),
                        Authors = x.Authors.Select(x => new
                        {
                            x.Id,
                            x.Name,
                            x.LastName,
                            x.Image
                        })
                    })
                    .FirstAsync());
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
                _context.Remove(await _context.Books.Include(x => x.Categories)
                    .Where(x => x.Categories.Any(y => y.Id == category) && x.Id == id)
                    .FirstAsync());
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
                Book bookdb = await _context.Books
                    .SingleAsync(x => x.Id == book.Id);
                string msg = "";

                foreach (PropertyInfo prop in book.GetType().GetProperties().ToArray())
                {
                    var propVal = prop.GetValue(book) ?? null;
                    if (propVal != null && !propVal.Equals("") && !propVal.Equals(0)
                        && (propVal as ICollection)?.Count != 0
                        && bookdb.GetType().GetProperty(prop.Name) != null)
                    {
                        if (prop.Name == "Name" || prop.Name == "LastName" || prop.Name == "Image")
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
                                        List<Category> newCats = _context.Categories
                                            .Where(c => book.Categories!
                                            .Select(cat => cat.Id).Contains(c.Id)).ToList();
                                        if (newCats.Count != 0)
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
                                        List<Author> newAuthors = _context.Authors
                                            .Where(
                                                c => book.Authors!
                                                .Select(cat => cat.Id)
                                                .Contains(c.Id))
                                            .ToList();
                                        if (newAuthors.Count != 0)
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
                                        List<Publisher> newPublishers = _context.Publishers
                                            .Where(
                                                c => book.Publishers!
                                                .Select(cat => cat.Id)
                                                .Contains(c.Id))
                                            .ToList();
                                        if (newPublishers.Count != 0)
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

        [HttpPost, Route("books/add")]
        public async Task<IActionResult> CreateBook(Book book)
        {
            bool check = _context.Authors.Any(a => book.Authors.Any(author => author == a));
            if (book != null && _context.Books.Include(b => b.Authors)
                .Any(b => b.Name == book.Name && check))
            {
                if (book.Image != null && book.Image.Length != 0)
                {
                    using (System.IO.MemoryStream ms = new MemoryStream(book.Image)) //Görselin kalitesi düşürülür
                    {
                        using (SixLabors.ImageSharp.Image image = Image.Load(ms))
                        {
                            image.Mutate(x => x.Resize(new ResizeOptions
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

                List<Author> matchedAuthors = await _context.Authors
                    .Where(a => book.Authors!
                    .Select(author => author.Id)
                        .Contains(a.Id))
                    .ToListAsync();

                List<Publisher> matchedPublishers = await _context.Publishers
                    .Where(p => book.Publishers!
                    .Select(publisher => publisher.Id)
                        .Contains(p.Id))
                    .ToListAsync();

                List<Category> matchedCategories = await _context.Categories
                    .Where(c => book.Categories!
                    .Select(cat => cat.Id)
                        .Contains(c.Id))
                    .ToListAsync();

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
    }
}
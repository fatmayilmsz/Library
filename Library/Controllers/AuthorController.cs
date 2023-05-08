using Library.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace Library.Controllers
{
    [ApiController]
    public class AuthorController : ControllerBase
    {
        private readonly LibraryContext _context;
        public AuthorController(LibraryContext librarycontext)
        {
            _context = librarycontext;
        }

        [HttpGet, Route("authors")]
        public async Task<IActionResult> FindAuthors()
        {
            return Ok(await _context.Authors.AsNoTracking().ToListAsync());
        }


        [HttpGet, Route("authors/{id}")]
        public async Task<IActionResult> GetAuthor(UInt32 id)
        {
            try
            {
                return Ok(await _context.Authors
                    .Include(x => x.Categories)
                    .Include(x => x.Books)
                    .AsNoTracking()
                    .Where(x => x.Id == id)
                    .Select(x => new
                    {
                        x.Id,
                        x.Name,
                        x.LastName,
                        x.Image,
                        Categories = x.Categories.Select(x => new
                        {
                            x.Id,
                            x.Name
                        }),
                        Books = x.Books.Select(x => new
                        {
                            x.Id,
                            x.Name,
                            x.Summary,
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


        [HttpDelete, Route("authors/delete/{id}")]
        public async Task<IActionResult> DeleteAuthor(UInt32 id)
        {
            try
            {
                _context.Authors.Remove(await _context.Authors.Where(x => x.Id == id).FirstAsync());
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception)
            {

                return NotFound();
            }
        }

        [HttpPost, Route("authors/add")]
        public async Task<IActionResult> AddAuthor(Author author)
        {
            try
            {
                await _context.Authors.AddAsync(author);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception)
            {

                return BadRequest();
            }
        }

        [HttpPut, Route("authors/update")]
        public async Task<IActionResult> UpdateAuthor(Author author)
        {
            try
            {
                Author authordb = await _context.Authors.Include(a => a.Categories)
                    .Include(a => a.Books).SingleAsync(x => x.Id == author.Id);
                string msg = "";

                foreach (PropertyInfo prop in author.GetType().GetProperties().ToArray())
                {
                    var propVal = prop.GetValue(author) ?? null;
                    if (propVal != null && !propVal.Equals("")
                        && !propVal.Equals(0) && (propVal as ICollection)?.Count != 0
                        && authordb.GetType().GetProperty(prop.Name) != null)
                    {
                        if (prop.Name == "Name" || prop.Name == "LastName" || prop.Name == "Image")
                        {
                            authordb.GetType()?.GetProperty(prop.Name)?.SetValue(authordb, propVal);
                        }
                        else
                        {
                            switch (prop.Name)
                            {
                                case "Categories":
                                    if (author.Categories != null)
                                    {
                                        List<Category> newCats = _context.Categories
                                            .Where(c => author.Categories!
                                            .Select(cat => cat.Id).Contains(c.Id)).ToList();
                                        if (newCats.Count != 0)
                                        {
                                            authordb.Categories?.Clear();
                                            authordb.Categories = newCats;
                                        }
                                        else
                                        {
                                            msg += "Category Update Failed - No Category Matches\n";
                                        }
                                    }
                                    break;
                                case "Books":
                                    if (author.Books != null)
                                    {
                                        List<Book> newBooks = _context.Books
                                            .Where(c => author.Books!
                                            .Select(cat => cat.Id)
                                            .Contains(c.Id)).ToList();
                                        if (newBooks.Count != 0)
                                        {
                                            authordb.Books?.Clear();
                                            authordb.Books = newBooks;
                                        }
                                        else
                                        {
                                            msg += "Book Update Failed - No Book Matches\n";
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }
                _context.Authors.Update(authordb);
                await _context.SaveChangesAsync();
                return Ok(new { message = msg });
            }
            catch (Exception ex)
            {

                return NotFound(ex.Message);
            }
        }
    }
}

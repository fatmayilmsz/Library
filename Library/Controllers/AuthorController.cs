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

        [HttpPost, Route("authors/add")]
        public async Task<IActionResult> AddAuthor(Author author)
        {
            if (!await _context.Authors.AnyAsync(a => a.Name == author.Name && a.LastName == author.LastName))
            {
                await _context.Authors.AddAsync(author);
                await _context.SaveChangesAsync();
                return Ok();
            }
            return BadRequest(new { message = "The author is already in database!" });
        }

        [HttpGet, Route("authors")]
        public async Task<IActionResult> FindAuthors()
        {
            return Ok(await _context.Authors.AsNoTracking().ToArrayAsync());
        }


        [HttpGet, Route("authors/{id}")]
        public async Task<IActionResult> GetAuthor(UInt32 id)
        {
            try
            {
                return Ok(await _context.Authors
                    .Include(a => a.Categories)
                    .Include(a => a.Books)
                    .AsNoTracking()
                    .Where(a => a.Id == id)
                    .Select(a => new
                    {
                        a.Id,
                        a.Name,
                        a.LastName,
                        a.Image,
                        Categories = a.Categories.Select(c => new
                        {
                            c.Id,
                            c.Name
                        }),
                        Books = a.Books.Select(b => new
                        {
                            b.Id,
                            b.Name,
                            b.Summary,
                            b.Image
                        })
                    })
                    .FirstAsync());
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        [HttpPut, Route("authors/update")]
        public async Task<IActionResult> UpdateAuthor(Author author)
        {
            try
            {
                Author authordb = await _context.Authors.Include(a => a.Categories)
                    .Include(a => a.Books).SingleAsync(a => a.Id == author.Id);
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
                                        Category[] newCats = await _context.Categories
                                            .Where(c => author.Categories!
                                            .Select(cat => cat.Id).Contains(c.Id)).ToArrayAsync();
                                        if (newCats.Length != 0)
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
                                        Book[] newBooks = await _context.Books
                                            .Where(c => author.Books!
                                            .Select(cat => cat.Id)
                                            .Contains(c.Id)).ToArrayAsync();
                                        if (newBooks.Length != 0)
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

        [HttpDelete, Route("authors/delete/{id}")]
        public async Task<IActionResult> DeleteAuthor(UInt32 id)
        {
            try
            {
                Author tempAuthor = await _context.Authors
                    .Include(a => a.Books)
                    .Include(a => a.Categories)
                    .Where(a => a.Id == id).FirstAsync();
                tempAuthor.Categories.Clear();
                tempAuthor.Books.Clear();
                _context.Authors.Remove(tempAuthor);
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

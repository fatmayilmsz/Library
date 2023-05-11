using Library.Models;
using Library.utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Reflection;

namespace Library.Controllers
{
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly LibraryContext _context;
        public CategoryController(LibraryContext librarycontext)
        {
            _context = librarycontext;
        }

        [HttpPost, Route("categories/add")]
        public async Task<IActionResult> AddCategory(Category category)
        {
            if (!await _context.Categories.AnyAsync(a => a.Name == category.Name))
            {
                await _context.Categories.AddAsync(category);
                await _context.SaveChangesAsync();
                return Ok();
            }
            return BadRequest(new { message = "The category is already in database!" });
        }

        [HttpGet, Route("categories")]
        public async Task<IActionResult> FindCategories()
        {
            return Ok(await _context.Categories.AsNoTracking().ToArrayAsync());
        }

        [HttpGet, Route("categories/{id}")]
        public async Task<IActionResult> GetCategory(UInt32 id)
        {
            try
            {
                return Ok(await _context.Categories
                    .Include(c => c.Books)
                    .Include(c => c.Authors)
                    .AsNoTracking()
                    .Where(c => c.Id == id)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        Books = c.Books.Select(book => new
                        {
                            book.Id,
                            book.Name,
                            book.Summary,
                            book.Image
                        }),
                        Authors = c.Authors.Select(author => new
                        {
                            author.Id,
                            author.Name,
                            author.LastName,
                            author.Image
                        })
                    })
                    .FirstAsync());
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        [HttpPut, Route("categories/update")]
        public async Task<IActionResult> UpdateCategory(Category category)
        {
            try
            {
                Category categorydb = await _context.Categories
                    .SingleAsync(c => c.Id == category.Id);
                string msg = "";

                foreach (PropertyInfo prop in category.GetType().GetProperties().ToArray())
                {
                    var propVal = prop.GetValue(category) ?? null;
                    if (propVal != null && !propVal.Equals("") && !propVal.Equals(0)
                        && (propVal as ICollection)?.Count != 0
                        && categorydb.GetType().GetProperty(prop.Name) != null)
                    {
                        if (LcUtils.GetDbSetTypes(_context).Any(dbSetType => dbSetType.Name != prop.Name))
                        {
                            categorydb.GetType()?.GetProperty(prop.Name)?.SetValue(categorydb, propVal);
                        }
                        else
                        {
                            switch (prop.Name)
                            {
                                case "Books":
                                    if (category.Books != null)
                                    {
                                        Book[] newBooks = await _context.Books
                                            .Where(c => category.Books!
                                            .Select(b => b.Id).Contains(c.Id)).ToArrayAsync();
                                        if (newBooks.Length != 0)
                                        {
                                            categorydb.Books?.Clear();
                                            categorydb.Books = newBooks;
                                        }
                                        else
                                        {
                                            msg += "Book Update Failed - No Book Matches\n";
                                        }
                                    }
                                    break;
                                case "Authors":
                                    if (category.Authors != null)
                                    {
                                        Author[] newAuthors = await _context.Authors
                                            .Where(
                                                a => category.Authors!
                                                .Select(a => a.Id)
                                                .Contains(a.Id))
                                            .ToArrayAsync();
                                        if (newAuthors.Length != 0)
                                        {
                                            categorydb.Authors?.Clear();
                                            categorydb.Authors = newAuthors;
                                        }
                                        else
                                        {
                                            msg += "Author Update Failed - No Author Matches\n";
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }

                _context.Categories.Update(categorydb);
                await _context.SaveChangesAsync();
                return Ok(new { message = msg });
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        [HttpDelete, Route("categories/delete/{id}")]
        public async Task<IActionResult> DeleteCategory(UInt32 id)
        {
            try
            {
                Category tempCat = await _context.Categories
                    .Include(c => c.Authors)
                    .Include(c => c.Books)
                    .Where(c => c.Id == id).FirstAsync();
                tempCat.Books.Clear();
                tempCat.Authors.Clear();
                _context.Categories.Remove(tempCat);
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

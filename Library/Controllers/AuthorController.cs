using Library.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            return Ok(await _context.Authors.ToListAsync());
        }

        [HttpGet, Route("authors/{id}")]
        public async Task<IActionResult> GetAuthor(UInt32 id)
        {
            try
            {
                return Ok(await _context.Authors.Where(x => x.Id == id).FirstAsync());
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
        public async Task<IActionResult> AddAuthors(Author author)
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
                Author authordb = await _context.Authors.Where(x => x.Id == author.Id).FirstAsync();

                foreach (PropertyInfo prop in author.GetType().GetProperties())
                {
                    var propVal = prop.GetValue(author);
                    if (propVal != null && !propVal.Equals("") && !propVal.Equals(0))
                    {
                        if (authordb.GetType().GetProperty(prop.Name) != null)
                        {
                            authordb.GetType().GetProperty(prop.Name).SetValue(authordb, propVal);
                        }
                    }
                }
                _context.Authors.Update(authordb);
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

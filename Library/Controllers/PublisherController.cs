using Library.Models;
using Library.utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Reflection;

namespace Library.Controllers
{
    [ApiController]
    public class PublisherController : ControllerBase
    {
        private readonly LibraryContext _context;
        public PublisherController(LibraryContext librarycontext)
        {
            _context = librarycontext;
        }

        [HttpPost, Route("publishers/add")]
        public async Task<IActionResult> AddCategory(Publisher publisher)
        {
            if (!await _context.Publishers.AnyAsync(p => p.Name == publisher.Name))
            {
                await _context.Publishers.AddAsync(publisher);
                await _context.SaveChangesAsync();
                return Ok();
            }
            return BadRequest(new { message = "The publisher is already in database!" });
        }

        [HttpGet, Route("publishers")]
        public async Task<IActionResult> FindPublishers()
        {
            return Ok(await _context.Publishers.AsNoTracking().ToArrayAsync());
        }

        [HttpGet, Route("publishers/{id}")]
        public async Task<IActionResult> GetPublisher(UInt32 id)
        {
            try
            {
                return Ok(await _context.Publishers
                    .Include(p => p.Books)
                    .AsNoTracking()
                    .Where(p => p.Id == id)
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        Books = p.Books.Select(book => new
                        {
                            book.Id,
                            book.Name,
                            book.Summary,
                            book.Image,
                            book.Categories
                        })
                    })
                    .FirstAsync());
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        [HttpPut, Route("publishers/update")]
        public async Task<IActionResult> UpdatePublisher(Publisher publisher)
        {
            try
            {
                Publisher publisherdb = await _context.Publishers
                    .SingleAsync(p => p.Id == publisher.Id);
                string msg = "";

                foreach (PropertyInfo prop in publisher.GetType().GetProperties().ToArray())
                {
                    var propVal = prop.GetValue(publisher) ?? null;
                    if (propVal != null && !propVal.Equals("") && !propVal.Equals(0)
                        && (propVal as ICollection)?.Count != 0
                        && publisherdb.GetType().GetProperty(prop.Name) != null)
                    {
                        if (LcUtils.GetDbSetTypes(_context).Any(dbSetType => dbSetType.Name != prop.Name))
                        {
                            publisherdb.GetType()?.GetProperty(prop.Name)?.SetValue(publisherdb, propVal);
                        }
                        else
                        {
                            switch (prop.Name)
                            {
                                case "Books":
                                    if (publisher.Books != null)
                                    {
                                        Book[] newBooks = await _context.Books
                                            .Where(c => publisher.Books!
                                            .Select(b => b.Id).Contains(c.Id)).ToArrayAsync();
                                        if (newBooks.Length != 0)
                                        {
                                            publisherdb.Books?.Clear();
                                            publisherdb.Books = newBooks;
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

                _context.Publishers.Update(publisherdb);
                await _context.SaveChangesAsync();
                return Ok(new { message = msg });
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        [HttpDelete, Route("publishers/delete/{id}")]
        public async Task<IActionResult> DeletePublisher(UInt32 id)
        {
            try
            {
                Publisher tempPublisher = await _context.Publishers
                    .Include(p => p.Books)
                    .Where(p => p.Id == id).FirstAsync();
                tempPublisher.Books.Clear();
                _context.Publishers.Remove(tempPublisher);
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

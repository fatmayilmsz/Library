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

        /// <summary>
        /// Yayıncı ekler, eklenen yayıncı onaysızdır.
        /// Koleksiyon halindeki DbSet tipleri eğer atama yapılmayacaksa
        /// boş liste halinde gönderilmelidir.
        /// </summary>
        [HttpPost, Route("publishers/add")]
        public async Task<IActionResult> AddCategory([FromBody] Publisher publisher)
        {
            CustomResponseBody crb = new CustomResponseBody();
            if (!await _context.Publishers.AnyAsync(p => p.Name == publisher.Name))
            {

                Book[] matchedBooks = await _context.Books
                    .Where(b => publisher.Books!
                    .Select(book => book.Id)
                        .Contains(b.Id))
                    .ToArrayAsync();

                if (
                    !matchedBooks.Any(b => publisher
                    .Books
                    .Select(book => book.Id)
                        .Contains(b.Id))
                    )
                {
                    crb.Warning += "Some/all specified publishers are not in db\n";
                }
                await _context.Publishers.AddAsync(new Publisher
                {
                    Name = publisher.Name,
                    Books = matchedBooks,
                    Approved = false
                });
                crb.Success += "Publisher is added to collection.";
                await _context.SaveChangesAsync();
                crb.Success += "Changes are saved.";
                return Ok(crb);
            }
            crb.Error += "The publisher already in db or one or more collections are null. Try to assign an empty list to collections.";
            return BadRequest(crb);
        }

        /// <summary>
        /// Onaylı yayıncıları çeker, ayrıntılar yoktur.
        /// </summary>
        [HttpGet, Route("publishers")]
        public async Task<IActionResult> FindPublishers()
        {
            return Ok(await _context.Publishers.AsNoTracking().Where(p => p.Approved).ToArrayAsync());
        }

        /// <summary>
        /// Onaysız yayıncıları çeker, ayrıntılar yoktur.
        /// </summary>
        [HttpGet, Route("publishers/unapproved")]
        public async Task<IActionResult> FindUnapprovedPublishers()
        {
            return Ok(await _context.Publishers.AsNoTracking().Where(p => !p.Approved).ToArrayAsync());
        }

        /// <summary>
        /// Onaylı bir yayıncı çeker, tüm ayrıntıları vardır.
        /// Yalnızca onaylı ayrıntıları çeker.
        /// </summary>
        [HttpGet, Route("publishers/{id}")]
        public async Task<IActionResult> GetPublisher(UInt32 id)
        {
            try
            {
                return Ok(await _context.Publishers
                    .Include(p => p.Books)
                    .AsNoTracking()
                    .Where(p => p.Id == id && p.Approved)
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Approved,
                        Books = p.Books.Select(book => new
                        {
                            book.Id,
                            book.Name,
                            book.Summary,
                            book.Image,
                            book.Categories,
                            book.Approved
                        })
                    })
                    .FirstAsync());
            }
            catch (Exception ex)
            {
                return NotFound(new CustomResponseBody { Error = ex.Message });
            }
        }

        /// <summary>
        /// Onaysız bir yayıncı çeker, tüm ayrıntıları vardır.
        /// Onaylı ve onaysız ayrıntıları çeker.
        /// </summary>
        [HttpGet, Route("publishers/unapproved/{id}")]
        public async Task<IActionResult> GetUnapprovedPublisher(UInt32 id)
        {
            try
            {
                return Ok(await _context.Publishers
                    .Include(p => p.Books)
                    .AsNoTracking()
                    .Where(p => p.Id == id && !p.Approved)
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Approved,
                        Books = p.Books.Select(book => new
                        {
                            book.Id,
                            book.Name,
                            book.Summary,
                            book.Image,
                            book.Categories,
                            book.Approved
                        })
                    })
                    .FirstAsync());
            }
            catch (Exception ex)
            {
                return NotFound(new CustomResponseBody { Error = ex.Message });
            }
        }

        /// <summary>
        /// Belirtilen "id"ye sahip kaydı
        /// belirtilen property ve değeri neyse onu kayıt üzerinden günceller.
        /// "Approved" property'si güncellenecek olsa da olmasa da belirtilmeli
        /// ve istenen değer gönderilmelidir aksi halde sürekli "false" atanır.
        /// </summary>
        [HttpPut, Route("publishers/update")]
        public async Task<IActionResult> UpdatePublisher([FromBody] Publisher publisher)
        {
            CustomResponseBody crb = new CustomResponseBody();
            try
            {
                Publisher publisherdb = await _context.Publishers
                    .Include(p => p.Books)
                    .SingleAsync(p => p.Id == publisher.Id);

                foreach (PropertyInfo prop in publisher.GetType().GetProperties().ToArray())
                {
                    var propVal = prop.GetValue(publisher) ?? null;
                    if (propVal != null && !propVal.Equals("") && !propVal.Equals(0)
                        && (propVal as ICollection)?.Count != 0
                        && publisherdb.GetType().GetProperty(prop.Name) != null)
                    {
                        if (!LcUtils.GetDbSetTypes(_context).Any(dbSetType => dbSetType.Name == prop.Name))
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
                                            crb.Warning += "Book Update Failed - No Book Matches\n";
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }

                _context.Publishers.Update(publisherdb);
                crb.Success += "Record is updated.";
                await _context.SaveChangesAsync();
                crb.Success += "Changes are saved.";
                return Ok(crb);
            }
            catch (Exception ex)
            {
                crb.Error += ex.Message;
                return NotFound(crb);
            }
        }

        /// <summary>
        /// Belirtilen yayıncı veritabanından silinir.
        /// </summary>
        [HttpDelete, Route("publishers/delete/{id}")]
        public async Task<IActionResult> DeletePublisher(UInt32 id)
        {
            CustomResponseBody crb = new CustomResponseBody();
            try
            {
                Publisher tempPublisher = await _context.Publishers
                    .Include(p => p.Books)
                    .Where(p => p.Id == id).FirstAsync();
                tempPublisher.Books.Clear();
                crb.Success += "Assigned collection elements are deleted.";
                _context.Publishers.Remove(tempPublisher);
                crb.Success += "Record is deleted.";
                await _context.SaveChangesAsync();
                crb.Success += "Changes are saved.";
                return Ok(crb);
            }
            catch (Exception ex)
            {
                crb.Error += ex.Message;
                return NotFound(crb);
            }
        }
    }
}

using Library.Models;
using Library.utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.Collections;
using System.Data;
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

        /// <summary>
        /// Yazar ekler, eklenen yazar onaysızdır.
        /// </summary>
        [HttpPost, Route("authors/add")]
        public async Task<IActionResult> AddAuthor([FromBody] Author author)
        {
            CustomResponseBody crb = new CustomResponseBody();
            if (!await _context.Authors
                .AnyAsync(a => a.Name + a.Lastname == author.Name + author.Lastname))
            {

                if (author.Image != null && author.Image.Length != 0)
                {
                    using (System.IO.MemoryStream ms = new MemoryStream(author.Image)) //Görselin kalitesi düşürülür
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
                            author.Image = ms.ToArray();
                            crb.Success += "Image successfuly resized.";
                        }
                    }
                }

                author.Categories = author.Categories != null ? await _context.Categories
                    .Where(c => author.Categories!
                    .Select(cat => cat.Id)
                        .Contains(c.Id))
                    .ToArrayAsync() : null;

                author.Books = author.Books != null ? await _context.Books
                    .Where(b => author.Books!
                    .Select(cat => cat.Id)
                        .Contains(b.Id))
                    .ToArrayAsync() : null;

                await _context.Authors.AddAsync(new Author
                {
                    Name = author.Name,
                    Lastname = author.Lastname,
                    Image = author.Image,
                    Categories = author.Categories,
                    Books = author.Books,
                    Approved = false
                });

                crb.Success += "Author is added to collection.";
                await _context.SaveChangesAsync();
                crb.Success += "Changes are saved.";
                return Ok(crb);
            }
            crb.Error = "The author is already in database.";
            return BadRequest(crb);
        }

        /// <summary>
        /// Onaylı yazarları çeker, ayrıntılar yoktur.
        /// </summary>
        [HttpGet, Route("authors")]
        public async Task<IActionResult> FindAuthors()
        {
            return Ok(await _context.Authors.AsNoTracking().Where(a => a.Approved).ToArrayAsync());
        }

        /// <summary>
        /// Onaysız yazarları çeker, ayrıntılar yoktur.
        /// </summary>
        [HttpGet, Route("authors/unapproved")]
        public async Task<IActionResult> FindUnapprovedAuthors()
        {
            return Ok(await _context.Authors.AsNoTracking().Where(a => !a.Approved).ToArrayAsync());
        }

        /// <summary>
        /// Onaylı bir yazarı çeker, tüm ayrıntıları vardır.
        /// Yalnızca onaylı ayrıntıları çeker.
        /// </summary>
        [HttpGet, Route("authors/{id}")]
        public async Task<IActionResult> GetAuthor(UInt32 id)
        {
            try
            {
                return Ok(await _context.Authors
                    .Include(a => a.Categories)
                    .Include(a => a.Books)
                    .AsNoTracking()
                    .Where(a => a.Id == id && a.Approved)
                    .Select(a => new
                    {
                        a.Id,
                        a.Name,
                        a.Lastname,
                        a.Image,
                        a.Approved,
                        Categories = a.Categories
                        .Where(c => c.Approved)
                        .Select(c => new
                        {
                            c.Id,
                            c.Name,
                            c.Approved
                        }),
                        Books = a.Books
                        .Where(b => b.Approved)
                        .Select(b => new
                        {
                            b.Id,
                            b.Name,
                            b.Summary,
                            b.Image,
                            b.Approved
                        })
                    })
                    .FirstAsync());
            }
            catch (Exception ex)
            {
                return NotFound(new CustomResponseBody { Error = ex.Message});
            }
        }

        /// <summary>
        /// Onaysız bir yazarı çeker, tüm ayrıntıları vardır.
        /// Onaylı ve onaysız ayrıntıları çeker.
        /// </summary>
        [HttpGet, Route("authors/unapproved/{id}")]
        public async Task<IActionResult> GetUnapprovedAuthor(UInt32 id)
        {
            try
            {
                return Ok(await _context.Authors
                    .Include(a => a.Categories)
                    .Include(a => a.Books)
                    .AsNoTracking()
                    .Where(a => a.Id == id && !a.Approved)
                    .Select(a => new
                    {
                        a.Id,
                        a.Name,
                        a.Lastname,
                        a.Image,
                        a.Approved,
                        Categories = a.Categories
                        .Select(c => new
                        {
                            c.Id,
                            c.Name,
                            c.Approved
                        }),
                        Books = a.Books
                        .Select(b => new
                        {
                            b.Id,
                            b.Name,
                            b.Summary,
                            b.Image,
                            b.Approved
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
        [HttpPut, Route("authors/update")]
        public async Task<IActionResult> UpdateAuthor([FromBody] Author author)
        {
            CustomResponseBody crb = new CustomResponseBody();
            try
            {
                Author authordb = await _context.Authors.Include(a => a.Categories)
                    .Include(a => a.Books).SingleAsync(a => a.Id == author.Id);

                foreach (PropertyInfo prop in author.GetType().GetProperties().ToArray())
                {
                    var propVal = prop.GetValue(author) ?? null;
                    if (propVal != null && !propVal.Equals("")
                        && !propVal.Equals(0) && (propVal as ICollection)?.Count != 0
                        && authordb.GetType().GetProperty(prop.Name) != null)
                    {
                        if (!LcUtils.GetDbSetTypes(_context).Any(dbSetType => dbSetType.Name == prop.Name))
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
                                            crb.Warning += "Category Update Failed - No Category Matches\n";
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
                                            crb.Warning += "Book Update Failed - No Book Matches\n";
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }
                _context.Authors.Update(authordb);
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
        /// Belirtilen yazar veritabanından silinir.
        /// </summary>
        [HttpDelete, Route("authors/delete/{id}")]
        public async Task<IActionResult> DeleteAuthor(UInt32 id)
        {
            CustomResponseBody crb = new CustomResponseBody();
            try
            {
                Author tempAuthor = await _context.Authors
                    .Include(a => a.Books)
                    .Include(a => a.Categories)
                    .Where(a => a.Id == id).FirstAsync();
                tempAuthor.Categories.Clear();
                tempAuthor.Books.Clear();
                crb.Success += "Assigned collection elements are deleted.";
                _context.Authors.Remove(tempAuthor);
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

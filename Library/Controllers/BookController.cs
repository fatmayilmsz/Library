using Library.Models;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using SixLabors.ImageSharp.Formats.Jpeg;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using Library.utils;

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

        /// <summary>
        /// Kitap ekler, eklenen kitap onaysızdır.
        /// </summary>
        [HttpPost, Route("books/add")]
        public async Task<IActionResult> AddBook([FromBody] Book book)
        {
            CustomResponseBody crb = new CustomResponseBody();
            //bool check = book.Authors?.Count != 0 && book.Authors != null ? await _context.Authors.AnyAsync(a => book.Authors.Any(auth => auth == a)) : true;
            if (book != null && !await _context.Books
                .Include(b => b.Authors)
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
                            crb.Success += "Image successfuly resized.";
                        }
                    }
                }

                book.Authors = book.Authors != null ? await _context.Authors
                    .Where(a => book.Authors!
                    .Select(author => author.Id)
                        .Contains(a.Id))
                    .ToArrayAsync() : null;

                book.Publishers = book.Publishers != null ? await _context.Publishers
                    .Where(p => book.Publishers!
                    .Select(publisher => publisher.Id)
                        .Contains(p.Id))
                    .ToArrayAsync() : null;

                book.Categories = book.Categories != null ? await _context.Categories
                    .Where(c => book.Categories!
                    .Select(cat => cat.Id)
                        .Contains(c.Id))
                    .ToArrayAsync() : null;

                book.Users = book.Users != null ? await _context.Users
                    .Where(u => book.Users!
                    .Select(user => user.Id)
                        .Contains(u.Id))
                    .ToArrayAsync() : null;

                await _context.Books.AddAsync(new Book()
                {
                    Name        =   book.Name,
                    Authors     =   book.Authors,
                    Publishers  =   book.Publishers,
                    Categories  =   book.Categories,
                    Users       =   book.Users,
                    Summary     =   book.Summary,
                    Image       =   book.Image,
                    Approved    =   false
                });

                crb.Success += "Book is added to collection.";
                await _context.SaveChangesAsync();
                crb.Success += "Changes are saved.";
                return Accepted(crb);
            }
            crb.Error += "The book already in database.";
            return BadRequest(crb);
        }

        /// <summary>
        /// Onaylı kitapları çeker, ayrıntılar yoktur.
        /// </summary>
        [HttpGet, Route("books")]
        public async Task<IActionResult> FindBooks()
        {
            return Ok(await _context.Books.AsNoTracking()
                .Where(b => b.Approved).ToArrayAsync());
        }

        /// <summary>
        /// Onaysız kitapları çeker, ayrıntılar yoktur.
        /// </summary>
        [HttpGet, Route("books/unapproved")]
        public async Task<IActionResult> FindUnapprovedBooks()
        {
            return Ok(await _context.Books.AsNoTracking()
                .Where(b => !b.Approved).ToArrayAsync());
        }

        /// <summary>
        /// Onaylı bir kitabı çeker, tüm ayrıntıları vardır.
        /// Yalnızca onaylı ayrıntıları çeker.
        /// </summary>
        [HttpGet, Route("books/{id}")]
        public async Task<IActionResult> GetBook(UInt32 id)
        {
            try
            {
                return Ok(await _context.Books
                    .Include(b => b.Categories)
                    .Include(b => b.Authors)
                    .Include(b => b.Publishers)
                    .AsNoTracking()
                    .Where(b => b.Id == id && b.Approved)
                    .Select(b => new
                    {
                        b.Id,
                        b.Name,
                        b.Summary,
                        b.Image,
                        b.Approved,
                        Categories = b.Categories
                        .Where(c => c.Approved)
                        .Select(c => new
                        {
                            c.Id,
                            c.Name,
                            c.Approved
                        }),
                        Authors = b.Authors
                        .Where(a => a.Approved)
                        .Select(a => new
                        {
                            a.Id,
                            a.Name,
                            a.Lastname,
                            a.Image,
                            a.Approved
                        }),
                        Publishers = b.Publishers
                        .Where(p => p.Approved)
                        .Select(p => new
                        { 
                            p.Id,
                            p.Name,
                            p.Approved
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
        /// Onaysız bir kitabı çeker, tüm ayrıntıları vardır.
        /// Onaylı ve onaysız ayrıntıları çeker.
        /// </summary>
        [HttpGet, Route("books/unapproved/{id}")]
        public async Task<IActionResult> GetUnapprovedBook(UInt32 id)
        {
            try
            {
                return Ok(await _context.Books
                    .Include(b => b.Categories)
                    .Include(b => b.Authors)
                    .Include(b => b.Publishers)
                    .AsNoTracking()
                    .Where(b => b.Id == id && !b.Approved)
                    .Select(b => new
                    {
                        b.Id,
                        b.Name,
                        b.Summary,
                        b.Image,
                        b.Approved,
                        Categories = b.Categories
                        .Select(c => new
                        {
                            c.Id,
                            c.Name,
                            c.Approved
                        }),
                        Authors = b.Authors
                        .Select(a => new
                        {
                            a.Id,
                            a.Name,
                            a.Lastname,
                            a.Image,
                            a.Approved
                        }),
                        Publishers = b.Publishers
                        .Select(p => new
                        {
                            p.Id,
                            p.Name,
                            p.Approved
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
        [HttpPut, Route("books/update")]
        public async Task<IActionResult> UpdateBook([FromBody] Book book)
        {
            // KATEGORİ ATAMA PROBLEMLİ
            CustomResponseBody crb = new CustomResponseBody();
            try
            {
                Book bookdb = await _context.Books
                    .Include(b => b.Categories)
                    .Include(b => b.Authors)
                    .Include(b => b.Users)
                    .SingleAsync(b => b.Id == book.Id);

                foreach (PropertyInfo prop in book.GetType().GetProperties().ToArray())
                {
                    var propVal = prop.GetValue(book) ?? null;
                    if (propVal != null && !propVal.Equals("") && !propVal.Equals(0)
                        && (propVal as ICollection)?.Count != 0
                        && bookdb.GetType().GetProperty(prop.Name) != null)
                    {
                        if (!LcUtils.GetDbSetTypes(_context).Any(dbSetType => dbSetType.Name == prop.Name))
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
                                            crb.Warning += "Category Update Failed - No Category Matches.";
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
                                            crb.Warning += "Author Update Failed - No Author Matches.";
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
                                            crb.Warning += "Publisher Update Failed - No Publisher Matches.";
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }

                _context.Books.Update(bookdb);
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
        /// Belirtilen kitap veritabanından silinir.
        /// </summary>
        [HttpDelete, Route("books/delete/{id}")]
        public async Task<IActionResult> DeleteBook(UInt32 id)
        {
            CustomResponseBody crb = new CustomResponseBody();
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
                crb.Success += "Assigned collection elements are deleted.";
                _context.Remove(tempBook);
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
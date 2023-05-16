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

        /// <summary>
        /// Kategori ekler, eklenen kategori onaysızdır.
        /// Koleksiyon halindeki DbSet tipleri eğer atama yapılmayacaksa
        /// boş liste halinde gönderilmelidir.
        /// </summary>
        [HttpPost, Route("categories/add")]
        public async Task<IActionResult> AddCategory([FromBody] Category category)
        {
            CustomResponseBody crb = new CustomResponseBody();
            if (!await _context.Categories.AnyAsync(a => a.Name == category.Name))
            {
                Author[] matchedAuthors = await _context.Authors
                    .Where(a => category.Authors!
                    .Select(author => author.Id)
                        .Contains(a.Id))
                    .ToArrayAsync();

                Book[] matchedBooks = await _context.Books
                    .Where(b => category.Books!
                    .Select(cat => cat.Id)
                        .Contains(b.Id))
                    .ToArrayAsync();

                if (!matchedAuthors.Any(a => category
                    .Authors
                    .Select(author => author.Id)
                        .Contains(a.Id))
                    ||
                    !matchedBooks.Any(b => category
                    .Books
                    .Select(book => book.Id)
                        .Contains(b.Id))
                    )
                {
                    crb.Warning += "Some/all specified authors or books are not in db.";
                }

                await _context.Categories.AddAsync(new Category
                {
                    Name = category.Name,
                    Books = matchedBooks,
                    Authors = matchedAuthors,
                    Approved = false
                });
                crb.Success += "Category is added to collection.";
                await _context.SaveChangesAsync();
                crb.Success += "Changes are saved.";
                return Ok(crb);
            }
            crb.Error += "The category already in db or one or more collections are null. Try to assign an empty list to collections.";
            return BadRequest(crb);
        }

        /// <summary>
        /// Onaylı kategorileri çeker, ayrıntılar yoktur.
        /// </summary>
        [HttpGet, Route("categories")]
        public async Task<IActionResult> FindCategories()
        {
            return Ok(await _context.Categories.AsNoTracking().Where(c => c.Approved).ToArrayAsync());
        }

        /// <summary>
        /// Onaysız kategorileri çeker, ayrıntılar yoktur.
        /// </summary>
        [HttpGet, Route("categories/unapproved")]
        public async Task<IActionResult> FindUnapprovedCategories()
        {
            return Ok(await _context.Categories.AsNoTracking().Where(c => !c.Approved).ToArrayAsync());
        }

        /// <summary>
        /// Onaylı bir kategoriyi çeker, tüm ayrıntıları vardır.
        /// Yalnızca onaylı ayrıntıları çeker.
        /// </summary>
        [HttpGet, Route("categories/{id}")]
        public async Task<IActionResult> GetCategory(UInt32 id)
        {
            try
            {
                return Ok(await _context.Categories
                    .Include(c => c.Books)
                    .Include(c => c.Authors)
                    .AsNoTracking()
                    .Where(c => c.Id == id && c.Approved)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Approved,
                        Books = c.Books
                        .Where(b => b.Approved)
                        .Select(b => new
                        {
                            b.Id,
                            b.Name,
                            b.Summary,
                            b.Image,
                            b.Approved
                        }),
                        Authors = c.Authors
                        .Where(a => a.Approved)
                        .Select(a => new
                        {
                            a.Id,
                            a.Name,
                            a.Lastname,
                            a.Image,
                            a.Approved
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
        /// Onaysız bir kategoriyi çeker, tüm ayrıntıları vardır.
        /// Onaylı ve onaysız ayrıntıları çeker.
        /// </summary>
        [HttpGet, Route("categories/unapproved/{id}")]
        public async Task<IActionResult> GetUnapprovedCategory(UInt32 id)
        {
            try
            {
                return Ok(await _context.Categories
                    .Include(c => c.Books)
                    .Include(c => c.Authors)
                    .AsNoTracking()
                    .Where(c => c.Id == id && !c.Approved)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Approved,
                        Books = c.Books
                        .Select(b => new
                        {
                            b.Id,
                            b.Name,
                            b.Summary,
                            b.Image,
                            b.Approved
                        }),
                        Authors = c.Authors
                        .Select(a => new
                        {
                            a.Id,
                            a.Name,
                            a.Lastname,
                            a.Image,
                            a.Approved
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
        [HttpPut, Route("categories/update")]
        public async Task<IActionResult> UpdateCategory([FromBody] Category category)
        {
            CustomResponseBody crb = new CustomResponseBody();
            try
            {
                Category categorydb = await _context.Categories
                    .Include(c => c.Books)
                    .Include(c => c.Authors)
                    .SingleAsync(c => c.Id == category.Id);

                foreach (PropertyInfo prop in category.GetType().GetProperties().ToArray())
                {
                    var propVal = prop.GetValue(category) ?? null;
                    if (propVal != null && !propVal.Equals("") && !propVal.Equals(0)
                        && (propVal as ICollection)?.Count != 0
                        && categorydb.GetType().GetProperty(prop.Name) != null)
                    {
                        if (!LcUtils.GetDbSetTypes(_context).Any(dbSetType => dbSetType.Name == prop.Name))
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
                                            crb.Warning += "Book Update Failed - No Book Matches.";
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
                                            crb.Warning += "Author Update Failed - No Author Matches.";
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }

                _context.Categories.Update(categorydb);
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
        /// Belirtilen kategori veritabanından silinir.
        /// </summary>
        [HttpDelete, Route("categories/delete/{id}")]
        public async Task<IActionResult> DeleteCategory(UInt32 id)
        {
            CustomResponseBody crb = new CustomResponseBody();
            try
            {
                Category tempCat = await _context.Categories
                    .Include(c => c.Authors)
                    .Include(c => c.Books)
                    .Where(c => c.Id == id).FirstAsync();
                tempCat.Books.Clear();
                tempCat.Authors.Clear();
                crb.Success += "Assigned collection elements are deleted.";
                _context.Categories.Remove(tempCat);
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

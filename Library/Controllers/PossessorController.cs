using Library.Models;
using Library.utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Library.Controllers
{
    public class PossessorController : ControllerBase
    {
        private readonly LibraryContext _context;
        public PossessorController(LibraryContext context)
        {
            _context = context;
        }

        [HttpGet, Route("possessors")]
        public async Task<IActionResult> FindPosessors()
        {
            return Ok(await _context.Users
                .AsNoTracking()
                .Where(u => u.Books.Count > 0)
                .Select(u => new
                {
                    u.Id,
                    Books = u.Books
                        .Select(u => new
                        {
                            u.Id
                        })
                })
                .ToArrayAsync());
        }

        [HttpGet, Route("possessors/{id}")]
        public async Task<IActionResult> GetPossessor(UInt32 id)
        {
            try
            {
                return Ok(await _context.Users
                    .Include(u => u.Books)
                    .AsNoTracking()
                    .Where(u => u.Id == id)
                    .Select(u => new
                    {
                        u.Id,
                        //u.Name,
                        //u.LastName,
                        //u.Email,
                        Books = u.Books
                        .Where(b => b.Approved)
                        .Select(b => new
                        {
                            b.Id,
                            //b.Name,
                            //b.Summary,
                            //b.Image,
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

        [HttpGet, Route("possessors/book/{id}")]
        public async Task<IActionResult> GetPossessorsViaBook(UInt32 id)
        {
            try
            {
                return Ok(await _context.Books
                    .Include(b => b.Categories)
                    .Include(b => b.Authors)
                    .Include(b => b.Publishers)
                    .Include(b => b.Users)
                    .AsNoTracking()
                    .Where(b => b.Id == id && b.Approved)
                    .Select(b => new
                    {
                        b.Id,
                        //b.Name,
                        //b.Summary,
                        //b.Image,
                        b.Approved,
                        //Categories = b.Categories
                        //.Select(c => new
                        //{
                        //    c.Id,
                        //    c.Name,
                        //    c.Approved
                        //}),
                        //Authors = b.Authors
                        //.Select(a => new
                        //{
                        //    a.Id,
                        //    a.Name,
                        //    a.Lastname,
                        //    a.Image,
                        //    a.Approved
                        //}),
                        //Publishers = b.Publishers
                        //.Select(p => new
                        //{
                        //    p.Id,
                        //    p.Name,
                        //    p.Approved
                        //}),
                        Users = b.Users
                        .Select(u => new
                        { 
                            u.Id,
                            u.Name,
                            u.LastName,
                            u.Email
                        })
                    })
                    .FirstAsync());
            }
            catch (Exception ex)
            {
                return NotFound(new CustomResponseBody { Error = ex.Message });
            }
        }

        [HttpPut, Route("possessors/assign")]
        //[Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdatePossessor([FromBody] User user)
        {
            CustomResponseBody crb = new CustomResponseBody();
            try
            {
                User userDb = await _context.Users
                    .Include(u => u.Books)
                    .SingleAsync(u => u.Id == user.Id);

                foreach (PropertyInfo prop in user.GetType().GetProperties().ToArray())
                {
                    var propVal = prop.GetValue(user) ?? null;
                    if (propVal != null && !propVal.Equals("")
                        && !propVal.Equals(0) && (propVal as ICollection)?.Count != 0
                        && userDb.GetType().GetProperty(prop.Name) != null)
                    {
                        if (LcUtils.GetDbSetTypes(_context).Any(dbSetType => dbSetType.Name == prop.Name))
                        {
                            switch (prop.Name)
                            {
                                case "Books":
                                    if (user.Books != null)
                                    {
                                        Book[] newBooks = await _context.Books
                                            .Where(b => user.Books!
                                            .Select(b => b.Id)
                                            .Contains(b.Id)).ToArrayAsync();
                                        if (newBooks.Length != 0)
                                        {
                                            userDb.Books?.Clear();
                                            userDb.Books = newBooks;
                                        }
                                        else
                                        {
                                            crb.Warning += "Book Update Failed - No Book Matches\n";
                                        }
                                    }
                                    break;
                            }
                        }
                        //else
                        //{
                        //    userDb.GetType()?.GetProperty(prop.Name)?.SetValue(userDb, propVal);
                        //}
                    }
                }
                _context.Users.Update(userDb);
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

        [HttpDelete, Route("possessors/delete/{id}")]
        public async Task<IActionResult> DeletePossessor(UInt32 id)
        {
            CustomResponseBody crb = new CustomResponseBody();
            try
            {
                User tempPossessor = await _context.Users
                    .Include(a => a.Books)
                    .Where(a => a.Id == id).FirstAsync();
                tempPossessor.Books.Clear();
                crb.Success += "Assigned collection elements are deleted.";
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

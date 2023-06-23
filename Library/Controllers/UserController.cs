using Library.Models;
using Library.utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Reflection;

namespace Library.Controllers
{
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly LibraryContext _context;
        public UserController(LibraryContext librarycontext)
        {
            _context = librarycontext;
        }

        [HttpPost, Route("users/add")]
        //[Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> AddUser([FromBody] User user)
        {
            CustomResponseBody crb = new CustomResponseBody();
            if (!await _context.Users
                .AnyAsync(u => u.Email == user.Email))
            {
                await _context.Users.AddAsync(user);
                crb.Success += "User is added to collection.";
                await _context.SaveChangesAsync();
                crb.Success += "Changes are saved.";
                return Ok(crb);
            }
            crb.Error = "The user is already in database.";
            return BadRequest(crb);
        }

        [HttpGet, Route("users")]
        //[Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> FindUsers()
        {
            return Ok(await _context.Users.AsNoTracking().ToArrayAsync());
        }

        [HttpPut, Route("users/update")]
        //[Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdateUser([FromBody] User user)
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
                        if (!LcUtils.GetDbSetTypes(_context).Any(dbSetType => dbSetType.Name == prop.Name))
                        {
                            if (prop.Name != "Role" || prop.Name == "Role" && (byte)propVal != 0)
                            {
                                userDb.GetType()?.GetProperty(prop.Name)?.SetValue(userDb, propVal);
                            }
                        }
                        //else
                        //{
                        //    switch (prop.Name)
                        //    {
                        //        case "Books":
                        //            if (user.Books != null)
                        //            {
                        //                Book[] newBooks = await _context.Books
                        //                    .Where(b => user.Books!
                        //                    .Select(b => b.Id)
                        //                    .Contains(b.Id)).ToArrayAsync();
                        //                if (newBooks.Length != 0)
                        //                {
                        //                    userDb.Books?.Clear();
                        //                    userDb.Books = newBooks;
                        //                }
                        //                else
                        //                {
                        //                    crb.Warning += "Book Update Failed - No Book Matches\n";
                        //                }
                        //            }
                        //            break;
                        //    }
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

        [HttpDelete, Route("users/delete/{id}")]
        //[Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> DeleteUser(UInt32 id)
        {
            CustomResponseBody crb = new CustomResponseBody();
            try
            {
                User tempUser = await _context.Users
                    .Include(a => a.Books)
                    .Where(a => a.Id == id).FirstAsync();
                tempUser.Books.Clear();
                crb.Success += "Assigned collection elements are deleted.";
                _context.Users.Remove(tempUser);
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

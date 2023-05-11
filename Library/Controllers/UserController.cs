using Library.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        [HttpGet, Route("users"), Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> FindUsers()
        {
            return Ok(await _context.Users.AsNoTracking().ToArrayAsync());
        }

        [HttpDelete, Route("users/delete/{id}"), Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> DeleteUser(UInt32 id)
        {
            try
            {
                _context.Users.Remove(await _context.Users.Where(u => u.Id == id).FirstAsync());
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception)
            {

                return NotFound();
            }
        }

        [HttpPost, Route("users/add"), Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> AddUser(User user)
        {
            try
            {
                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception)
            {

                return BadRequest();
            }
        }

        [HttpPut, Route("users/update"), Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdateUser(User user)
        {
            try
            {
                User userdb = await _context.Users.Where(u => u.Id == user.Id).FirstAsync();

                foreach (PropertyInfo prop in user.GetType().GetProperties())
                {
                    var propVal = prop.GetValue(user);
                    if (propVal != null && !propVal.Equals("") && !propVal.Equals(0))
                    {
                        if (userdb.GetType().GetProperty(prop.Name) != null)
                        {
                            userdb.GetType().GetProperty(prop.Name).SetValue(userdb, propVal);
                        }
                    }
                }
                _context.Users.Update(userdb);
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

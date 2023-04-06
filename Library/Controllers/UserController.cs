using Library.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Library.Controllers
{
    [ApiController]
    [Route("users")]
    public class UserController : ControllerBase
    {
        private readonly LibraryContext _context;
        public UserController(LibraryContext librarycontext)
        {
            _context = librarycontext;
        }

        [HttpGet, Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> FindUsers()
        {
            return Ok(await _context.Users.ToListAsync());
        }

        [HttpGet, Route("/")]
        public async Task<IActionResult> GetFavs()
        {
            return Ok(await _context.Users.ToListAsync());
        }
    }
}

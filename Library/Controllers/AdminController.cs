using Library.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.Controllers
{
    [ApiController]
    [Route("admins")]
    public class AdminController : ControllerBase
    {
        private readonly LibraryContext _context;
        public AdminController(LibraryContext librarycontext)
        {
            _context = librarycontext;
        }

        [HttpGet,Authorize(Roles = "SuperAdmin")]
        public IActionResult FindAdmins()
        {
            var admins = _context.Admins.ToList();
            return Ok(admins);
        }
    }
}

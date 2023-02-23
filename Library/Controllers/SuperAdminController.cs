using Library.Models;
using Microsoft.AspNetCore.Mvc;

namespace Library.Controllers
{
    [ApiController]
    [Route("superadmins")]
    public class SuperAdminController : ControllerBase
    {
        private readonly LibraryContext _context;
        public SuperAdminController(LibraryContext librarycontext)
        {
            _context = librarycontext;
        }

        [HttpGet]
        public IActionResult FindSuperAdmins()
        {
            var superadmins = _context.SuperAdmins.ToList();
            return Ok(superadmins);
        }
    }
}

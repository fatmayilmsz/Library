using Library.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.Controllers
{
    [ApiController]
    [Route("superadmins")]
    public class SuperAdminController : ControllerBase
    {
        private readonly LibraryContext _context;
        private readonly IConfiguration _configuration;

        public SuperAdminController(LibraryContext librarycontext, IConfiguration configuration)
        {
            _context = librarycontext;
            _configuration = configuration;
        }

        //public async Task<IActionResult> FindSuperAdmins()
        //{
        //     var superadmins = _context.SuperAdmins.FindAsync();
        //     return Ok(await superadmins);
        //}
        [HttpGet, Authorize(Roles = "SuperAdmin")]

        public IActionResult FindSuperAdmins()
        {
            var superadmins = _context.SuperAdmins.ToList();
            return Ok( superadmins);
        }
    }
}

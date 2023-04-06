using Library.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library.Controllers
{
    [ApiController]
    [Route("categories")]
    public class CategoryController : ControllerBase
    {
        private readonly LibraryContext _context;
        public CategoryController(LibraryContext librarycontext)
        {
            _context = librarycontext;
        }

        [HttpGet]
        public async Task<IActionResult> FindCategories()
        {
            return Ok(await _context.Categories.ToListAsync());
        }
    }
}

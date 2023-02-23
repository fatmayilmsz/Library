using Library.Models;
using Microsoft.AspNetCore.Mvc;

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
        public IActionResult FindCategories()
        {
            var categories = _context.Categories.ToList();
            return Ok(categories);
        }
    }
}

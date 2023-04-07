using Library.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        [HttpGet, Route("categories")]
        public async Task<IActionResult> FindCategories()
        {
            return Ok(await _context.Categories.ToListAsync());
        }

        [HttpDelete, Route("categories/delete/{id}")]
        public async Task<IActionResult> DeleteCategory(UInt32 id)
        {
            try
            {
                _context.Categories.Remove(await _context.Categories.Where(x => x.Id == id).FirstAsync());
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception)
            {

                return NotFound();
            }
        }

        [HttpPost, Route("categories/add")]
        public async Task<IActionResult> AddCategory(Category category)
        {
            try
            {
                await _context.Categories.AddAsync(category);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception)
            {

                return BadRequest();
            }
        }

        [HttpPut, Route("categories/update")]
        public async Task<IActionResult> UpdateCategory (Category category)
        {
            try
            {
                Category categorydb = await _context.Categories.Where(x => x.Id == category.Id).FirstAsync();

                foreach (PropertyInfo prop in category.GetType().GetProperties())
                {
                    var propVal = prop.GetValue(category);
                    if (propVal != null && !propVal.Equals("") && !propVal.Equals(0))
                    {
                        if (categorydb.GetType().GetProperty(prop.Name) != null)
                        {
                            categorydb.GetType().GetProperty(prop.Name).SetValue(categorydb, propVal);
                        }
                    }
                }
                _context.Categories.Update(categorydb);
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

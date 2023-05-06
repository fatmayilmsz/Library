using Library.Models;
using Microsoft.AspNetCore.Mvc;

namespace Library.Controllers
{
    public class AddressController : ControllerBase
    {
        private readonly LibraryContext _context;
        public AddressController(LibraryContext context)
        {
            _context = context;
        }
        [HttpPost, Route("address/add")]
        public async Task<IActionResult> AddAddress([FromBody] Address address)
        {
            if(address != null) 
            {
                await _context.Address.AddAsync(new Address()
                {
                    Id= address.Id,
                    FullName= address.FullName,
                    Title= address.Title,
                    City= address.City,
                    District= address.District,
                    Neighborhood= address.Neighborhood,
                    BuildingNumber= address.BuildingNumber,
                    FloorNumber= address.FloorNumber,
                    ApartmentNumber= address.ApartmentNumber,
                    DeliveryAddress= address.DeliveryAddress,
                    PostCode= address.PostCode,
                    PhoneNumber= address.PhoneNumber
                });
                await _context.SaveChangesAsync();
            }
            else
            {
                return BadRequest("Invalid data.");
            }
            return Ok();
        }
    }
}

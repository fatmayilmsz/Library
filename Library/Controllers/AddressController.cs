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
            CustomResponseBody crb = new CustomResponseBody();
            if (address != null) 
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
                crb.Success += "Address is added to collection.";
                await _context.SaveChangesAsync();
                crb.Success += "Changes are saved.";
            }
            else
            {
                crb.Error += "Address is null, invalid data.";
                return BadRequest(crb);
            }
            return Ok(crb);
        }
    }
}

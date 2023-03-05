using Library.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Library.Controllers
{
    public class AuthController : ControllerBase
    {
        public static UserDto userDto = new UserDto();

        private readonly IConfiguration _configuration;
        private readonly LibraryContext _context;


        public AuthController(LibraryContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        [HttpPost("register")]
        public IActionResult Post([FromBody] User request)
        {
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            if (!ModelState.IsValid)
                return BadRequest("Invalid data.");

            _context.Users.Add(new User()
            {
                Name = request.Name,
                LastName = request.LastName,
                Email = request.Email,
                Password = passwordHash,
                Role= request.Role,
            });
            _context.SaveChanges();
            return Ok();
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Authenticate([FromBody] AuthRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var userInDb = _context.Users.FirstOrDefault(u => u.Email == request.Email);
            if (userInDb is null) return BadRequest("Kullanıcıya ulaşılamadı");

            //var managedUser = await _userManager.FindByEmailAsync(request.Email);
            //if (managedUser == null)
            //{
            //    return BadRequest("Bad credentials");
            //}
            //string passwordHashh = BCrypt.Net.BCrypt.HashPassword(userInDb.Password);
            userDto.Id=userInDb.Id;
            userDto.Name = userInDb.Name;
            userDto.LastName = userInDb.LastName;
            userDto.Email = userInDb.Email;
            userDto.PasswordHash = userInDb.Password;
            userDto.Role = userInDb.Role;

            if (!BCrypt.Net.BCrypt.Verify(request.Password, userDto.PasswordHash))
            {
                return BadRequest("Wrong password.");
            }
            //var isPasswordValid = await _userManager.CheckPasswordAsync(managedUser, request.Password);
            //if (!isPasswordValid)
            //{
            //    return BadRequest("Bad credentials");
            //}


            var accessToken = CreateToken(userDto);
            await _context.SaveChangesAsync();
            return Ok(new AuthResponse
            {
                Name = userInDb.Name,
                LastName = userInDb.LastName,
                Email = userInDb.Email,
                PasswordHash = userDto.PasswordHash, 
                Role= userDto.Role,
                Token = (string)accessToken,
            });
        }
        private string CreateToken(UserDto user)
        {

            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name,user.Name),

            };

            if (user.Role == 1)
            {
                claims.Add(new Claim(ClaimTypes.Role, "User")); 
            }
            else if(user.Role == 2)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }
            else if(user.Role == 3) 
            {
                claims.Add(new Claim(ClaimTypes.Role, "SuperAdmin"));
            }

      
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("AppSettings:Token").Value!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
                );
            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }


    }
}
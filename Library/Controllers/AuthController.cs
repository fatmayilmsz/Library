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
        public async Task<IActionResult> Post([FromBody] User request)
        {
            if (ModelState.IsValid)
            {
                userDto.Id = request.Id;
                userDto.Name = char.ToUpper(request.Name.Trim().ToLower()[0]) + request.Name.Trim().ToLower().Substring(1);
                userDto.LastName = char.ToUpper(request.LastName.Trim().ToLower()[0]) + request.LastName.Trim().ToLower().Substring(1);
                userDto.Email = request.Email.Trim();
                userDto.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password.Trim());
                userDto.Role = _context.Roles.Single(r => r.Name == "user").Id;

                _context.Users.Add(new User()
                {
                    Name = userDto.Name,
                    LastName = userDto.LastName,
                    Email = userDto.Email,
                    Password = userDto.PasswordHash,
                    Role = userDto.Role,
                });

                await _context.SaveChangesAsync();

                return Ok(new AuthResponse
                {
                    Name = userDto.Name,
                    LastName = userDto.LastName,
                    Email = userDto.Email,
                    PasswordHash = userDto.PasswordHash,
                    Role = userDto.Role,
                    Token = (string)CreateToken(userDto),
                });
            }
            return BadRequest("Invalid data.");
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Authenticate([FromBody] AuthRequest request)
        {
            if (ModelState.IsValid)
            {
                Library.Models.User userInDb;
                try
                {
                    userInDb = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email.Trim());
                }
                catch (Exception)
                {
                    return BadRequest("Kullanıcıya ulaşılamadı");
                }

                if (BCrypt.Net.BCrypt.Verify(request.Password, userInDb.Password))
                {
                    userDto.Id = userInDb.Id;
                    userDto.Name = userInDb.Name;
                    userDto.LastName = userInDb.LastName;
                    userDto.Email = userInDb.Email;
                    userDto.PasswordHash = userInDb.Password;
                    userDto.Role = userInDb.Role;

                    await _context.SaveChangesAsync();
                    return Ok(new AuthResponse
                    {
                        Name = userDto.Name,
                        LastName = userDto.LastName,
                        Email = userDto.Email,
                        PasswordHash = userDto.PasswordHash,
                        Role = userDto.Role,
                        Token = (string)CreateToken(userDto),
                    });
                }
                return BadRequest("Wrong password.");
            }
            return BadRequest(ModelState);
        }
        private string CreateToken(UserDto user)
        {
            List<Claim> claims = new List<Claim> // Burada neden List var? Array olursa daha iyi olmaz mı? ya da tupple(?)
            {
                new Claim(ClaimTypes.Name,user.Name),
            };

            switch (user.Role)
            {
                case 1:
                    claims.Add(new Claim(ClaimTypes.Role, "User"));
                    break;
                case 2:
                    claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                    break;
                case 3:
                    claims.Add(new Claim(ClaimTypes.Role, "SuperAdmin"));
                    break;
            }

            System.IdentityModel.Tokens.Jwt.JwtSecurityToken token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("AppSettings:Token").Value!)), SecurityAlgorithms.HmacSha512Signature)
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


    }
}
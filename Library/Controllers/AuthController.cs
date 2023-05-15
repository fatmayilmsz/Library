using Library.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Net;
using System.Net.Mail;

namespace Library.Controllers
{
    public class AuthController : ControllerBase
    {
        public static UserDto userDto = new UserDto();

        private readonly IConfiguration _configuration;
        private readonly LibraryContext _context;
        private readonly IWebHostEnvironment _env;

        public AuthController(LibraryContext context, IConfiguration configuration, IWebHostEnvironment env)
        {
            _context = context;
            _configuration = configuration;
            _env = env;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Post([FromBody] User request)
        {
            CustomResponseBody crb = new CustomResponseBody();
            if (ModelState.IsValid)
            {
                crb.Success += "Model is valid.";
                userDto.Id = request.Id;
                userDto.Name = char.ToUpper(request.Name.Trim().ToLower()[0]) + request.Name.Trim().ToLower().Substring(1);
                userDto.LastName = char.ToUpper(request.LastName.Trim().ToLower()[0]) + request.LastName.Trim().ToLower().Substring(1);
                userDto.Email = request.Email.Trim();
                userDto.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password.Trim());
                userDto.Role = _context.Roles.Single(r => r.Name == "user").Id;

                string conCode = GenerateConfirmationCode();
                SmtpModel smtpModel = new SmtpModel()
                {
                    SenderEmail = "onlinelibraryconfirmation@gmail.com",
                    SenderPassword = "",
                    SmtpServer = "sandbox.smtp.mailtrap.io",
                    SmtpPort = 2525,
                    To = userDto.Email,
                    Subject = "Online Kütüphane Onay",
                    Body = $"\r\n" +
                        $"<h1>Online Kütüphane Onay Kodu</h1>\r\n" +
                        $"<p>Merhaba {userDto.Name}!</p>\r\n" +
                        $"<p>Onay kodunuz: <strong>{conCode}</strong></p>\r\n" +
                        $"<p>Lütfen bu kodu kullanarak hesabınızı onaylayın.</p>\r\n" +
                        $"<p>Teşekkürler!</p>\r\n",
                    ToName = userDto.Name,
                    ConfirmationCode = conCode
                };
                
                SendConfirmationEmail(smtpModel);
                crb.Success += "Confirmation mail has sent.";

                _context.Users.Add(new User()
                {
                    Name = userDto.Name,
                    LastName = userDto.LastName,
                    Email = userDto.Email,
                    Password = userDto.PasswordHash,
                    Role = userDto.Role,
                    Approved = false
                });

                crb.Success += "The user added to collection.";
                await _context.SaveChangesAsync();
                crb.Success += "Record is saved.";

                return Ok(new AuthResponse
                {
                    Name = userDto.Name,
                    LastName = userDto.LastName,
                    Email = userDto.Email,
                    PasswordHash = userDto.PasswordHash,
                    Role = userDto.Role,
                    Token = (string)CreateToken(userDto)
                });
            }
            crb.Error += "Invalid data.";
            return BadRequest(crb);
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Authenticate([FromBody] AuthRequest request)
        {
            CustomResponseBody crb = new CustomResponseBody();
            if (ModelState.IsValid)
            {
                crb.Success += "Model is valid.";
                Library.Models.User userInDb;
                try
                {
                    userInDb = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == request.Email.Trim());
                }
                catch (Exception ex)
                {
                    crb.Error += ex.Message;
                    return BadRequest(crb);
                }

                if (BCrypt.Net.BCrypt.Verify(request.Password, userInDb.Password))
                {
                    crb.Success += "Password verified.";
                    userDto.Id = userInDb.Id;
                    userDto.Name = userInDb.Name;
                    userDto.LastName = userInDb.LastName;
                    userDto.Email = userInDb.Email;
                    userDto.PasswordHash = userInDb.Password;
                    userDto.Role = userInDb.Role;
                    userDto.Approved = userInDb.Approved;

                    return Ok(new AuthResponse
                    {
                        Name = userDto.Name,
                        LastName = userDto.LastName,
                        Email = userDto.Email,
                        PasswordHash = userDto.PasswordHash,
                        Role = userDto.Role,
                        Token = (string)CreateToken(userDto),
                        Approved = userDto.Approved
                    });
                }
                crb.Error += "Wrong password.";
                return BadRequest(crb);
            }
            return BadRequest(ModelState);
        }
        private string CreateToken(UserDto user)
        {
            CustomResponseBody crb = new CustomResponseBody();
            List<Claim> claims = new List<Claim> // Burada neden List var? Array olursa daha iyi olmaz mı? ya da tupple(?)
            {
                new Claim(ClaimTypes.Name,user.Name),
            };
            crb.Success += "Claim list is created.";
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

            crb.Success += $"{user.Role} is assigned.";

            System.IdentityModel.Tokens.Jwt.JwtSecurityToken token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("AppSettings:Token").Value!)), SecurityAlgorithms.HmacSha512Signature)
                );

            crb.Success += "Token is created.";
            // CRB BURADA DÖNDÜRÜLEMİYOR, DÜZENLENMELİ!!!
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private class SmtpModel
        {
            public string SenderEmail { get; set; }
            public string SenderPassword { get; set; }
            public string SmtpServer { get; set; }
            public int SmtpPort { get; set; }
            public string To { get; set; }
            public string Subject { get; set; }
            public string Body { get; set; }
            public string ToName { get; set; }
            public string ConfirmationCode { get; set; }
        }

        private void SendConfirmationEmail(SmtpModel smtpModel)
        {
            SmtpClient smtpClient = new SmtpClient(smtpModel.SmtpServer, smtpModel.SmtpPort);
            smtpClient.EnableSsl = true;
            smtpClient.Credentials = new NetworkCredential("f31d89af1fcdb6", "fcc53ece690e3e");

            MailMessage mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(smtpModel.SenderEmail);
            mailMessage.To.Add(smtpModel.To);
            mailMessage.Subject = smtpModel.Subject;
            mailMessage.Body = smtpModel.Body;
            mailMessage.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(smtpModel.Body, null, "text/html"));

            smtpClient.Send(mailMessage);
        }

        private string GenerateConfirmationCode()
        {
            const int codeLength = 6;
            const string allowedChars = "0123456789";
            StringBuilder codeBuilder = new StringBuilder();

            Random random = new Random();
            for (int i = 0; i < codeLength; i++)
            {
                int randomIndex = random.Next(0, allowedChars.Length);
                codeBuilder.Append(allowedChars[randomIndex]);
            }

            return codeBuilder.ToString();
        }

    }
}
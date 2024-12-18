using DatabaseContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Kafedra.Controllers
{
    [ApiController]
    [Route("/")]
    public class LoginController : Controller
    {
        private IConfiguration _configuration;

        public LoginController(IConfiguration configuration)
        {
            _configuration = configuration;
            Hash.configuration = configuration;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest loginRequest)
        {
            if (loginRequest.username.Length < 1)
            {
                return Json(new
                {
                    success = false,
                    message = "Bad username"
                });
            }
            else if (loginRequest.password.Length < 1)
            {
                return Json(new
                {
                    success = false,
                    message = "Bad password"
                });
            }

            var user = AppDbContext.Instance.Users.Where(u => u.Username == loginRequest.username).FirstOrDefault();
            if (user == null)
            {
                return Json(new
                {
                    success = false,
                    message = "There is no user " + loginRequest.username
                });
            }

            if (user.Password == Hash.GetHash(loginRequest.password))
            {
                var token = Hash.GenerateJwtToken(user.Username);
                AppDbContext.Instance.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Login success",
                    token = token,
                    isStudent = user.IsStudent
                });
            }
            else
            {
                return Json(new
                {
                    success = false,
                    message = "Wrong password"
                });
            }
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegistrationRequest registerRequest)
        {
            if (registerRequest.username.Length < 1)
            {
                return Json(new
                {
                    success = false,
                    message = "Bad username"
                });
            }
            else if (registerRequest.password.Length < 1)
            {
                return Json(new
                {
                    success = false,
                    message = "Bad password"
                });
            }

            var user = AppDbContext.Instance.Users.Where(u => u.Username == registerRequest.username).FirstOrDefault();
            if (user != null)
            {
                return Json(new
                {
                    success = false,
                    message = "Already registered"
                });
            }

            User newUser = new()
            {
                Username = registerRequest.username,
                Password = Hash.GetHash(registerRequest.password),
                IsStudent = registerRequest.isStudent
            };
            AppDbContext.Instance.Users.Add(newUser);
            AppDbContext.Instance.SaveChanges();

            if (newUser.IsStudent)
            {
                Student student = new()
                {
                    StudentName = newUser.Username,
                    Status = StudentStatus.pendingApproval,
                    Group = new Random().Next(110, 120).ToString()
                };
                AppDbContext.Instance.Students.Add(student);
                AppDbContext.Instance.SaveChanges();
            }

            var token = Hash.GenerateJwtToken(registerRequest.username);

            return Json(new
            {
                success = true,
                message = "Register success",
                token = token
            });
        }

        [HttpGet("TokenCheck")]
        public IActionResult TockenCheck()
        {
            var token = Request.Headers["Authorization"];
            var username = Hash.GetUsernameFromJwtToken(token);
            if (!Hash.IsUserExists(username))
                return Unauthorized();

            var user = AppDbContext.Instance.Users.FirstOrDefault(x => x.Username == username);
            if (user == null)
                return Unauthorized();

            if (user.IsStudent)
                return Redirect("/studentPage");
            else
                return Redirect("/StudentsManagment");
        }

        public class LoginRequest
        {
            public string username { get; set; }
            public string password { get; set; }
        }

        public class RegistrationRequest
        {
            public string username { get; set; }
            public string password { get; set; }
            public bool isStudent { get; set; }
        }
    }
}

public class Hash
{
    public static IConfiguration configuration;

    public static string GetHash(string data)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(data);
            byte[] hashBytes = sha256.ComputeHash(byteArray);
            StringBuilder hexString = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                hexString.AppendFormat("{0:x2}", b);
            }

            return hexString.ToString();
        }
    }

    public static string GenerateJwtToken(string username)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var claims = new[]
        {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, username)
            };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpiresInMinutes"])),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string GetUsernameFromJwtToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();

        try
        {
            var jwtSettings = configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],

                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],

                ValidateLifetime = true,

                IssuerSigningKey = key,
            };

            var principal = handler.ValidateToken(token, tokenValidationParameters, out var validatedToken);
            var usernameClaim = principal?.FindFirst(ClaimTypes.Name);
            if (usernameClaim != null)
            {
                return usernameClaim.Value;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return "null";
        }

        return "null";
    }

    public static bool IsUserExists(string username)
    {
        var user = AppDbContext.Instance.Users.Where(u => u.Username == username).FirstOrDefault();

        return user != null;
    }
}
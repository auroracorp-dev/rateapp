using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using WebAPI.Context;
using WebAPI.Manager;
using WebAPI.Models;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthManager _manager;
        private readonly IConfiguration _config;
        // TEMP STATIC CURRENT USER LINE
        public static User? CurrentUser { get; private set; } 
        public AuthController(AuthManager manager, IConfiguration config)
        {
            _manager = manager;
            _config = config;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLogIn request)
        {
            try
            {
                var user = await _manager.TryToLogin(request, CurrentUser);

                if (user == null)
                {
                    return Unauthorized(new { message = "잘못된 접근입니다." });
                }
                var token = GenerateJwtToken(user);

                return Ok(new
                {
                    token,
                    user.deviceSN
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message }); // 409
            }
            catch (ArgumentException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }
        [Authorize]
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            return Ok("Logged out");
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _config["Jwt:Key"];

            if (string.IsNullOrEmpty(jwtKey))
                throw new Exception("Jwt:Key is missing in configuration");

            var key = new SymmetricSecurityKey(Convert.FromBase64String(jwtKey));

            var creds = new SigningCredentials(key,SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.userName.ToString()),
                new Claim("deviceSN", user.deviceSN)
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

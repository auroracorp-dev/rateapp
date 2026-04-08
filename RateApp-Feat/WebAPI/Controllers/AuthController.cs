using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        // TEMP STATIC CURRENT USER LINE
        public static User? CurrentUser { get; private set; } 
        public AuthController(AuthManager manager)
        {
            _manager = manager;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLogIn request)
        {
            try
            {
                var user = await _manager.TryToLogin(request, CurrentUser);
                CurrentUser = user;

                return Ok(new { user.deviceSN });
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
        [HttpPost("logout")]
        public IActionResult LogOut()
        {
            try
            {
                _manager.TryToLogOut(CurrentUser);

                // ⭐ 여기 핵심
                CurrentUser = null;

                return Ok("LOG OUT.");
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }
    }
}

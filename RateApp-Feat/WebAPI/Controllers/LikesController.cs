using Microsoft.AspNetCore.Mvc;
using WebAPI.Manager;
using WebAPI.Models;
using static Utils;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class review_likeController : ControllerBase
    {
        private readonly LikeManager _manager;

        public review_likeController(LikeManager manager)
        {
            _manager = manager;
        }

        [RequireUser]
        [HttpPost("toggle")]
        public async Task<ActionResult<object>> ToggleLike([FromBody] LikeInput input)
        {
            try
            {
                var result = await _manager.ToggleAsync(input);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }
    }
}
using Microsoft.AspNetCore.Authorization;
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

        [Authorize]
        [HttpPost("toggle")]
        public async Task<ActionResult<object>> ToggleLike([FromBody] LikeInput input)
        {
            var result = await _manager.ToggleAsync(input);
            return Ok(result);
        }
    }
}
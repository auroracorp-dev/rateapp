using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.Context;
using WebAPI.Manager;
using WebAPI.Models;
using static Utils;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class usersController : ControllerBase
    {
        private readonly UserManager _manager;

        public usersController(UserManager manager)
        {
            _manager = manager;
        }

        // POST: api/add
        [HttpPost("add")]
        public async Task<ActionResult<User>> PostUser([FromBody] UserInput input)
        {
            try
            {
                return await _manager.AddAsync(input);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message }); // 409
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // DELETE: api/delete/id
        [RequireUser]
        [HttpDelete("delete/{id}")]
        public async Task<ActionResult> DeleteUser(int id)
        {
            try
            {
                await _manager.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }

        // GET: api/getall
        [RequireUser]
        [HttpGet("getall")]
        public async Task<ActionResult<PagedResult<UserOutput>>> GetUserAll([FromQuery] UserFilter filter)
        {
            try
            {
                return await _manager.GetAllAsync(filter);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }

        // GET: api/get/id
        [HttpGet("get/{id}")]
        public async Task<ActionResult<UserOutput?>> GetUser(int id)
        {
            try
            {
                return await _manager.GetByIdAsync(id);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }

        // GET: api/get/id
        [HttpPatch("UpdateRole/{id}")]
        public async Task<ActionResult<UserOutput?>> UpdateUserRole(int id, [FromBody] UpdateRole role)
        {
            try
            {
                //var currentUser = AuthController.CurrentUser;
                //if (currentUser == null)
                //    return Unauthorized(new { message = "로그인이 필요합니다." });
                // var result = await _manager.UpdateUsersRole(id, role, currentUser);
                return Ok("result");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message }); // 409
            }
        }
    }
}

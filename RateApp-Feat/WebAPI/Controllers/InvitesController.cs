using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WebAPI.Context;
using WebAPI.Manager;
using WebAPI.Models;
using static Utils;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class inviteController : ControllerBase
    {
        private readonly InviteManager _manager;

        public inviteController(InviteManager manager)
        {
            _manager = manager;
        }


        [Authorize(Roles = "manager,superuser")]
        [HttpPost("manager/add/{count}")]
        public async Task<IActionResult> PostInvite(int count = 1)
        {
            try
            {
                await _manager.TryToAddCode(count);
                return Ok(new { message = $"{count}개의 초대 코드 생성 완료" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }

        // DELETE: api/delete/id
        [Authorize(Roles = "superuser")]
        [HttpDelete("superuser/delete/{id}")]
        public async Task<ActionResult> DeleteInvite(int id)
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
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message }); // 409
            }
        }

        // GET: api/getall
        [Authorize(Roles = "manager,superuser")]
        [HttpGet("manager/getall")]
        public async Task<ActionResult<PagedResult<InviteOutput>>> GetinviteAll([FromQuery] InviteFilter filter)
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

        // GET: api/get
        [Authorize]
        [HttpGet("get")]
        public async Task<ActionResult<InviteOutput?>> Getinvite()
        {
            try
            {
                return await _manager.GetByAsync();
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
    }
}

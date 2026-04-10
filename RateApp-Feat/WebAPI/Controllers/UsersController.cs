using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
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
            if (User.Identity?.IsAuthenticated == true)
                return Forbid("잘못된 접근입니다.");
            input.Validate();
            return await _manager.AddAsync(input);
        }

        // DELETE: api/delete/id
        [Authorize(Roles = "master,superuser")]
        [HttpDelete("master/delete/{id}")]
        public async Task<ActionResult> DeleteUser(int id)
        {
            await _manager.DeleteAsync(id);
            return NoContent();
        }

        // GET: api/getall
        [Authorize(Roles = "master,superuser")]
        [HttpGet("master/getall")]
        public async Task<ActionResult<PagedResult<UserOutput>>> GetUserAll([FromQuery] UserFilter filter)
        {
            return await _manager.GetAllAsync(filter);
        }

        // GET: api/get/id
        [Authorize(Roles ="master,superuser")]
        [HttpGet("master/get/{id}")]
        public async Task<ActionResult<UserOutput?>> GetUser(int id)
        {
            return await _manager.GetByIdAsync(id);
        }

        // GET: api/get/id
        [Authorize(Roles ="superuser")]
        [HttpPatch("supersuer/updaterole/{id}")]
        public async Task<ActionResult<UserOutput?>> UpdateUserRole(int id, [FromBody] UpdateRole role)
        {
            return Ok($"{id}유저의 {role.role} 변경 완료");
        }
    }
}

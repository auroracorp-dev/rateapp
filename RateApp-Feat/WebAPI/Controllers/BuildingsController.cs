using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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
    public class buildingController : ControllerBase
    {
        private readonly BuildingManager _manager;

        public buildingController(BuildingManager manager)
        {
            _manager = manager;
        }

        // POST: api/add
        [RequireUser]
        [HttpPost("add")]
        public async Task<ActionResult<Building>> PostBuilding([FromBody] BuildingInput input)
        {
            try
            {
                return await _manager.AddAsync(input);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message }); // 409
            }
        }


        // DELETE: api/delete/id
        [RequireUser]
        [HttpDelete("delete/{id}")]
        public async Task<ActionResult> DeleteBuilding(int id)
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

        // GET: api/getall/page
        [RequireUser]
        [HttpGet("getall/{curPage}")]
        public async Task<ActionResult<PagedResult<BuildingOutput>>> GetinviteAll([FromQuery] BuildingFilter filter, int curPage = 1)
        {
            try
            {
                return await _manager.GetAllAsync(filter, curPage);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }

        // GET: api/get/id
        [HttpGet("get/{id}")]
        public async Task<ActionResult<BuildingOutput?>> Getinvite(int id)
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
            catch (InvalidDataException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}

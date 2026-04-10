using Microsoft.AspNetCore.Authorization;
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
        [Authorize]
        [HttpPost("add")]
        public async Task<ActionResult<Building>> PostBuilding([FromBody] BuildingInput input)
        {
            input.Validate();
            return await _manager.AddAsync(input);
        }


        // DELETE: api/delete/id
        [Authorize(Roles = "master,superuser")]
        [HttpDelete("manager/delete/{id}")]
        public async Task<ActionResult> DeleteBuilding(int id)
        {
            await _manager.DeleteAsync(id);
            return NoContent();
        }

        // GET: api/getall
        [Authorize]
        [HttpGet("getall")]
        public async Task<ActionResult<PagedResult<BuildingOutput>>> GetBuildingAll([FromQuery] BuildingFilter filter)
        {
            return await _manager.GetAllAsync(filter);
        }

        // GET: api/get/id
        [Authorize]
        [HttpGet("get/{id}")]
        public async Task<ActionResult<BuildingOutput?>> GetBuilding(int id)
        {
            return await _manager.GetByIdAsync(id);
        }
    }
}

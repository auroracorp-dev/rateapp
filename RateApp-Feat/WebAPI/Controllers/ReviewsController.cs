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
    public class reviewController : ControllerBase
    {
        private readonly ReviewManager _manager;

        public reviewController(ReviewManager manager)
        {
            _manager = manager;
        }

        // POST: api/add
        [Authorize]
        [HttpPost("add")]
        public async Task<ActionResult<Review>> PostReview([FromBody] ReviewInput input)
        {
            input.Validate();
            return await _manager.AddAsync(input);
        }

        // DELETE: api/delete/id
        [Authorize]
        [HttpDelete("delete/{id}")]
        public async Task<ActionResult> DeleteReview(int id)
        {
            await _manager.DeleteAsync(id);
            return NoContent();
        }

        // GET: api/getall
        [Authorize]
        [HttpGet("getall")]
        public async Task<ActionResult<PagedResult<ReviewOutput>>> GetReviewAll([FromQuery] ReviewFilter filter)
        {
            return await _manager.GetAllAsync(filter);
        }

        // GET: api/get/id
        [Authorize]
        [HttpGet("get/{id}")]
        public async Task<ActionResult<ReviewOutput?>> GetReview(int id)
        {
            return await _manager.GetByIdAsync(id);
        }
    }
}

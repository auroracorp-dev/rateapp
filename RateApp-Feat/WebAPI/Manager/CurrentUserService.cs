using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebAPI.Context;
using WebAPI.Models;

namespace WebAPI.Manager;

public class CurrentUserService
{
    private readonly IHttpContextAccessor _accessor;
    private readonly AppDbContext _context;

    public CurrentUserService(IHttpContextAccessor accessor, AppDbContext context)
    {
        _accessor = accessor;
        _context = context;
    }

    public async Task<User?> GetUserAsync()
    {
        var userId = _accessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            return null;

        return await _context.users.FindAsync(Convert.ToInt32(userId));
    }
}

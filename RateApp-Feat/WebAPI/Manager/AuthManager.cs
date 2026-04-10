using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.Context;
using WebAPI.Manager.Interfaces;
using WebAPI.Models;

namespace WebAPI.Manager
{
    public class AuthManager
    {
        private readonly AppDbContext _context;
        public AuthManager(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> TryToLogin(UserLogIn request)
        {
            // DB에서 사용자 확인
            var user = await _context.users.SingleOrDefaultAsync(u => u.deviceSN == request.deviceSN.ToLower()) ??
                throw new ArgumentException("해당 기기 정보는 존재하지 않습니다.");

            return user;
        }
    }
}
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

        public async Task<User?> TryToLogin(UserLogIn request, User? CurrentUser)
        {
            if (CurrentUser != null)
                throw new InvalidOperationException("이미 로그인된 상태입니다.");

            // DB에서 사용자 확인
            var user = await _context.users.SingleOrDefaultAsync(u => u.deviceSN == request.deviceSN);

            if (user == null)
                throw new ArgumentException("해당 기기 정보는 존재하지 않습니다.");

            return user;
        }

        public void TryToLogOut(User? CurrentUser)
        {
            if (CurrentUser == null)
                throw new InvalidOperationException("로그인이 되어있지 않은 상태이므로, 로그아웃이 불가능합니다.");

            CurrentUser = null;

        }

    }
}
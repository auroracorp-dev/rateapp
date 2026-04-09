using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using WebAPI.Context;
using WebAPI.Controllers;
using WebAPI.Manager.Interfaces;
using WebAPI.Models;
using static Utils;

namespace WebAPI.Manager
{
    public class UserManager : IBaseManager<User, UserInput>, IGetAll<UserOutput, UserFilter>, IGetID<UserOutput>
    {
        private readonly AppDbContext _context;
        public UserManager(AppDbContext context)
        {
            _context = context;
        }
        // ================= PUBLIC ================= //
        public async Task<User> AddAsync(UserInput input, User? currentUser = null)
        {
            var user = AuthController.CurrentUser;
            if (user != null)
                throw new Exception("현재 로그인 되어진 계정이 있습니다. 잘못된 접근입니다.");
            // 권한 체크
            input.Validate();
            bool exists = await _context.users.AnyAsync(x => x.deviceSN == input.deviceSN || x.userName == input.userName);
            if (exists)
                throw new InvalidOperationException("이미 존재하는 사용자 정보입니다.");
            var invite = await _context.invite.FirstOrDefaultAsync(x => x.inviteCode == input.inviteCode) ??
                throw new KeyNotFoundException("해당 초대 코드가 존재하지 않습니다.");
            if (invite.IsUsed)
                throw new InvalidOperationException("이미 사용된 초대 코드입니다.");
            var newUser = new User(input);

            // 추가 처리
            using var tx = await _context.Database.BeginTransactionAsync();
            _context.users.Add(newUser);
            invite.SetUsed(newUser.userId);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return newUser;
        }
        public async Task DeleteAsync(int id, User? currentUser = null)
        {
            // 권한 체크
            var user = Utils.EnsureUser(currentUser);
            var targetUser = await GetUserOrThrow(id);
            
            //삭제 처리
            targetUser.CheckPermissionForDelete(user);
            using var tx = await _context.Database.BeginTransactionAsync();
            await ResetInvite(targetUser);
            await RemoveUserLikes(targetUser);
            await RemoveUserReviews(targetUser);
            await RemoveUserBuildings(targetUser);
            _context.users.Remove(targetUser);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        public async Task<Utils.PagedResult<UserOutput>> GetAllAsync(UserFilter filter, User? currentUser = null)
        {
            var user = Utils.EnsureUser(currentUser);
            IQueryable<User> query = _context.users.AsNoTracking();
            query = QueryFilter(filter, user, query);
            return new PagedResult<UserOutput>
            {
                Items = await PagingHelper.GetPageData(
                    query: query,
                    page: filter.page,
                    orderBy: i => i.userId,
                    selector: i => new UserOutput
                    {
                        userName = i.userName,
                        role = i.role,
                        Point = i.Point,
                        createTime = i.createTime,
                    }
                ),
                TotalCount = await query.CountAsync()
            };
        }
        public async Task<UserOutput?> GetByIdAsync(int id, User? currentUser = null)
        {
            Utils.EnsureUser(currentUser);
            var user = await GetUserOrThrow(id);
            return DTO<UserOutput, User>.Convert(user);
        }
        public async Task<UserOutput> UpdateUsersRole(int userId, UpdateRole urole, User currentUser)
        {
            // 권한 체크
            var user = Utils.EnsureUser(currentUser);
            if (!Utils.HasSuperUserPermission(user.role))
                throw new UnauthorizedAccessException("관리자 변경 권한 없음");
            var targetUser = await GetUserOrThrow(userId);
            if (targetUser.role == urole.role)
                throw new InvalidOperationException($"이미 {urole.role} 권한입니다.");
            // 변경
            targetUser.role = urole.role;
            await _context.SaveChangesAsync();

            return DTO<UserOutput, User>.Convert(targetUser);
        }
        // ================= PRIVATE ================= //
        private async Task<User> GetUserOrThrow(int id)
        {
            return await _context.users.FindAsync(id) ??
                throw new KeyNotFoundException("해당 유저 없음");
        }

        private IQueryable<User> QueryFilter(UserFilter filter, User user, IQueryable<User> query)
        {
            if (!Utils.HasManagerPermission(user.role))
                query = query.Where(u => u.userId == user.userId);
            if (filter.role.HasValue)
                query = query.Where(u => u.role == filter.role);
            if (!string.IsNullOrEmpty(filter.userName))
                query = query.Where(u => u.userName != null && EF.Functions.Like(u.userName, $"%{filter.userName}%"));

            query = query.Where(u => u.Point >= filter.MinPoint && u.Point <= filter.MaxPoint);

            return query;
        }
        // ================= DELETE SUB LOGIC ================= //
        private async Task ResetInvite(User targetUser)
        {
            var invite = await _context.invite
                .FirstOrDefaultAsync(x => x.inviteCode == targetUser.inviteCode);
            if (invite != null)
                invite.SetUsedNull();
        }
        private async Task RemoveUserLikes(User targetUser)
        {
            var likes = await _context.review_like.Where(l => l.userId == targetUser.userId).ToListAsync();
            var reviewIds = likes.Select(l => l.reviewId).Distinct();
            var reviewMap = await _context.review .Where(r => reviewIds.Contains(r.reviewId)) .ToDictionaryAsync(r => r.reviewId);
            foreach (var like in likes)
            {
                if (reviewMap.TryGetValue(like.reviewId, out var review))
                {
                    review.RemoveLike();
                }
            }
            _context.review_like.RemoveRange(likes);
        }
        private async Task RemoveUserReviews(User targetUser)
        {
            var reviews = await _context.review.Where(r => r.userId == targetUser.userId).ToListAsync();
            var buildingIds = reviews.Select(r => r.buildingId).Distinct();
            var buildings = await _context.building.Where(b => buildingIds.Contains(b.buildingId)).ToDictionaryAsync(b => b.buildingId);
            foreach (var review in reviews)
            {
                if (buildings.TryGetValue(review.buildingId, out var building))
                {
                    building.RemoveReview(review.rating);
                }
            }
            _context.review.RemoveRange(reviews);
        }
        private async Task RemoveUserBuildings(User targetUser)
        {
            var buildings = await _context.building.Where(b => b.uploadUserId == targetUser.userId).ToListAsync();
            _context.building.RemoveRange(buildings);
        }
    }
}

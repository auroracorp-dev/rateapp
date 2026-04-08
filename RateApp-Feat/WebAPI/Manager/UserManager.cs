using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using WebAPI.Context;
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

        public async Task<User> AddAsync(UserInput input, User? currentUser = null)
        {
            // 문자열 체크
            bool checkName = TextFilter.ContainsBadWord(input.userName);
            bool checkSn = TextFilter.ContainsBadWord(input.deviceSN);
            bool checkInv = TextFilter.ContainsBadWord(input.inviteCode);
            if (checkName || checkSn || checkInv)
                throw new InvalidOperationException("잘못된 입력입니다.");

            // 시리얼넘버 체크
            var sn = await _context.users.FirstOrDefaultAsync(x => x.deviceSN == input.deviceSN);
            if (sn != null)
                throw new InvalidOperationException("해당 기기 정보가 이미 존재합니다.");
            // 이미 존재하는 닉네임입니다.
            var userName = await _context.users.FirstOrDefaultAsync(x => x.userName == input.userName);
            if (userName != null)
                throw new InvalidOperationException("해당 사용자 이름이 이미 존재합니다.");
            // 초대 코드 체크
            var invite = await _context.invite.FirstOrDefaultAsync(x => x.inviteCode == input.inviteCode);
            if (invite == null)
                throw new KeyNotFoundException("해당 초대 코드가 존재하지 않습니다.");

            User newUser = new User
            {
                userName = input.userName,
                deviceSN = input.deviceSN,
                inviteCode = input.inviteCode,
                createTime = DateTime.UtcNow,
                role = Role.User,
                Point = 0,
            };

            // 새로운 유저 등록
            _context.users.Add(newUser);
            await _context.SaveChangesAsync();

            // 초대코드 사용처리
            invite.SetUsed(newUser.userId);
            await _context.SaveChangesAsync();

            return newUser;
        }

        public async Task DeleteAsync(int id, User? currentUser = null)
        {
            // 현재 유저 체크
            var user = Utils.EnsureUser(currentUser);

            var targetUser = await _context.users.FindAsync(id);
            if (targetUser == null)
                throw new KeyNotFoundException("해당 유저 없음");

            // 권한 체크
            bool isManager = Utils.HasManagerPermission(user.role);
            bool isOwner = user.userId == targetUser.userId;
            if (!isManager && !isOwner)
                throw new UnauthorizedAccessException("권한 없음");

            var invite = await _context.invite
                .FirstOrDefaultAsync(x => x.inviteCode == targetUser.inviteCode);

            if (invite == null)
                throw new KeyNotFoundException("초대코드 없이 만들어진 유저");

            // 초대 코드 초기화
            invite.SetUsedNull();

            var likes = await _context.review_like.Where(l => l.userId == targetUser.userId).ToListAsync();
            var reviewIds = likes.Select(l => l.reviewId).Distinct().ToList();
            var reviewMap = await _context.review.Where(r => reviewIds.Contains(r.reviewId)).ToDictionaryAsync(r => r.reviewId);

            // 추천
            foreach (var like in likes)
            {
                if (reviewMap.TryGetValue(like.reviewId, out var review))
                {
                    review.RemoveLike();
                }
            }
            _context.review_like.RemoveRange(likes);

            // 리뷰
            var reviews = await _context.review.Where(r => r.userId == targetUser.userId).ToListAsync();
            foreach (var review in reviews)
            {
                var building = await _context.building.FirstOrDefaultAsync(b => b.buildingId == review.buildingId);
                if (building != null)
                    building.RemoveReview(review.rating);
            }
            _context.review.RemoveRange(reviews);

            // 빌딩
            var buildings = await _context.building.Where(b => b.uploadUserId == targetUser.userId).ToListAsync();
            _context.building.RemoveRange(buildings);

            // 유저
            _context.users.Remove(targetUser);

            await _context.SaveChangesAsync();
        }

        public async Task<Utils.PagedResult<UserOutput>> GetAllAsync(UserFilter filter, User? currentUser = null)
        {
            var user = Utils.EnsureUser(currentUser);

            // 전체 쿼리
            IQueryable<User> query = _context.users;

            // 필터
            if (!Utils.HasManagerPermission(user.role))
            {
                query = query.Where(u => u.userId == user.userId);
            }
            if (filter.MinPoint.HasValue)
            {
                query = query.Where(u => u.Point >= filter.MinPoint.Value);
            }
            if (filter.MaxPoint.HasValue)
            {
                query = query.Where(u => u.Point >= filter.MaxPoint.Value);
            }
            if (filter.role.HasValue)
            {
                query = query.Where(u => u.role == filter.role);
            }
            if (!string.IsNullOrEmpty(filter.userName))
            {
                query = query.Where(u => u.userName != null &&
                    EF.Functions.Like(u.userName, $"%{filter.userName}%"));
            }

            // 전체 쿼리
            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(i => i.userId).Skip((filter.page - 1) * Utils.pageCount).Take(Utils.pageCount)
                .Select(i => new UserOutput
                {
                    userName = i.userName,
                    role = i.role,
                    Point = i.Point,
                    createTime = i.createTime,
                })
                .ToListAsync();

            return new PagedResult<UserOutput> { Items = items, TotalCount = totalCount };
        }

        public async Task<UserOutput?> GetByIdAsync(int id, User? currentUser = null)
        {
            // 현재 유저 체크
            var user = Utils.EnsureUser(currentUser);

            var users = await _context.users.FindAsync(id);
            if (users == null)
                throw new KeyNotFoundException("해당 유저 없음");

            // DB 반영
            await _context.SaveChangesAsync();

            // DTO 반환
            return DTO<UserOutput, User>.Convert(users);
        }
        public async Task<UserOutput> UpdateUserRole(int userId, UpdateRole urole, User currentUser)
        {
            var user = Utils.EnsureUser(currentUser);

            if (!Utils.HasSuperUserPermission(user.role))
                throw new UnauthorizedAccessException("관리자 변경 권한 없음");

            var targetUser = await _context.users.FindAsync(userId);

            if (targetUser == null)
                throw new KeyNotFoundException("해당 유저 없음");

            if (targetUser.role == urole.role)
                throw new InvalidOperationException($"해당 유저는 이미 {urole.role} 권한입니다.");

            targetUser.role = urole.role;

            await _context.SaveChangesAsync();

            // DTO 변환 후 반환
            return DTO<UserOutput, User>.Convert(targetUser);
        }
    }
}

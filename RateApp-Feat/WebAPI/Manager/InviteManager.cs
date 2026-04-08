using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using WebAPI.Context;
using WebAPI.Controllers;
using WebAPI.Manager.Interfaces;
using WebAPI.Models;
using static Utils;

namespace WebAPI.Manager
{
    public class InviteManager : IBaseManager<Invite, Unit>, IGetAll<InviteOutput, InviteFilter>, IGet<InviteOutput>
    {
        private readonly AppDbContext _context;
        public InviteManager(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Invite> AddAsync(Unit input, User? currentUser = null)
        {
            // 현재 유저 체크
            var user = Utils.EnsureUser(currentUser);

            // 권한 체크
            if (!Utils.HasSuperUserPermission(user.role))
                throw new UnauthorizedAccessException("초대 코드 생성 권한 없음");

            Invite invite = new Invite();
            bool saved = false;

            while (!saved)
            {
                string randomCode = Utils.GenerateInviteCode(user.userId);

                invite.inviteCode = randomCode;
                invite.createDate = DateTime.UtcNow;
                invite.createdBy = user.userId;
                invite.IsUsed = false;

                _context.invite.Add(invite);

                try
                {
                    await _context.SaveChangesAsync();
                    saved = true;
                }
                catch (DbUpdateException ex)
                {
                    _context.Entry(invite).State = EntityState.Detached;
                }
            }

            return invite;
        }

        public async Task DeleteAsync(int id, User? currentUser = null)
        {
            // 현재 유저 체크
            var user = Utils.EnsureUser(currentUser);

            var invite = await _context.invite.FindAsync(id);
            if (invite == null)
                throw new KeyNotFoundException("초대 코드 없음");

            // 권한 체크
            bool isManager = Utils.HasManagerPermission(user.role);
            bool isOwner = user.userId == invite.createdBy;
            if (!isManager && !isOwner)
                throw new UnauthorizedAccessException("권한 없음");

            // 사용중 체크
            if (invite.IsUsed)
                throw new InvalidOperationException("이미 사용된 초대 코드입니다.");

            // 제거 후 갱신
            _context.Remove(invite);
            await _context.SaveChangesAsync();
        }

        public async Task<PagedResult<InviteOutput>> GetAllAsync(InviteFilter filter, User? currentUser = null)
        {
            var user = Utils.EnsureUser(currentUser);

            var query = _context.invite.Include(i => i.Creator).AsQueryable();

            // 필터
            if (!Utils.HasManagerPermission(user.role))
            {
                query = query.Where(i => i.createdBy == user.userId);
            }
            if (filter.IsUsed.HasValue)
            {
                query = query.Where(i => i.IsUsed == filter.IsUsed.Value);
            }
            if (filter.createStartDate.HasValue)
            {
                query = query.Where(i => i.createDate >= filter.createStartDate.Value);
            }
            if (filter.createLastDate.HasValue)
            {
                query = query.Where(i => i.createDate <= filter.createLastDate.Value);
            }
            if (!string.IsNullOrEmpty(filter.createdByName))
            {
                query = query.Where(i => i.Creator != null &&
                    EF.Functions.Like(i.Creator.userName, $"%{filter.createdByName}%"));
            }

            // 전체 쿼리
            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(i => i.createDate) // 최신순 정렬
                .Skip((filter.page - 1) * Utils.pageCount) // 페이징
                .Take(Utils.pageCount)
                .Select(i => new InviteOutput
                {
                    inviteId = i.inviteId,
                    inviteCode = i.inviteCode, 
                    IsUsed = i.IsUsed,
                    createdByName = i.Creator != null ? i.Creator.userName : "알 수 없음",
                })
                .ToListAsync();

            return new PagedResult<InviteOutput> { Items = items, TotalCount = totalCount };
        }

        public async Task<InviteOutput?> GetByAsync(User? currentUser = null)
        {
            // 현재 유저 체크
            var user = Utils.EnsureUser(currentUser);

            // 코드 소유 체크 
            var existingInvite = await _context.invite.FirstOrDefaultAsync(i => i.usedBy == user.userId && !i.IsUsed);

            if (existingInvite != null)
                return DTO<InviteOutput, Invite>.Convert(existingInvite);

            // 사용되지 않은 코드 중 하나 가져오기
            var invite = await _context.invite.Where(i => i.IsUsed == false && i.usedBy == null)
                .OrderBy(i => i.inviteId).FirstOrDefaultAsync();

            if (invite == null)
                throw new KeyNotFoundException("여분 초대 코드 없음");

            // currentUser에게 할당
            invite.usedBy = user.userId;

            // DB 반영
            await _context.SaveChangesAsync();

            // DTO 반환
            return DTO<InviteOutput, Invite>.Convert(invite);
        }

        public async Task TryToAddCode(int count, User? currentUser = null)
        {
            var user = Utils.EnsureUser(currentUser);

            for (int i = 0; i < count; i++)
            {
                await AddAsync(new Unit(), currentUser);
            }
        }

    }
}

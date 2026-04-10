using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using System.Security.Authentication;
using System.Security.Claims;
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
        private readonly CurrentUserService _currentUserService;

        public InviteManager(AppDbContext context, CurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        // ============ PUBLIC FUNC LINE ============ //
        const int MAX_COUNT = 100;

        public async Task TryToAddCode(int count = 1, User? currentUser = null)
        {
            if (count <= 0 || count >= MAX_COUNT)
                throw new Exception("입력 범위 초과");

            for (int i = 0; i < count; i++)
            {
                await AddAsync(new Unit(), currentUser);
            }
        }
        public async Task<Invite> AddAsync(Unit input, User? currentUser = null)
        {
            var user = await CurrentUser();

            Invite invite = new Invite();
            string randomCode = await GenerateUniqueInviteCode(user);

            invite.SetInvite(user.userId, randomCode);

            _context.invite.Add(invite);
            await _context.SaveChangesAsync();

            return invite;
        }

        public async Task DeleteAsync(int id, User? currentUser = null)
        {
            var invite = await _context.invite.FindAsync(id) ??
                throw new KeyNotFoundException("초대 코드 없음");

            // 사용중 체크
            if (invite.IsUsed)
                throw new InvalidOperationException("이미 사용된 초대 코드입니다.");

            // 제거 후 갱신
            _context.Remove(invite);
            await _context.SaveChangesAsync();
        }

        public async Task<PagedResult<InviteOutput>> GetAllAsync(InviteFilter filter, User? currentUser = null)
        {
            var query = _context.invite.Include(i => i.Creator).AsQueryable();
            query = QueryFilter(filter, query);

            return new PagedResult<InviteOutput>
            {
                TotalCount = await query.CountAsync(),
                Items = await PagingHelper.GetPageData<Invite, InviteOutput, DateTime>(
                    query, filter.page, i => i.createDate, i => new InviteOutput
                    {
                        inviteId = i.inviteId,
                        inviteCode = i.inviteCode,
                        IsUsed = i.IsUsed,
                        createdByName = i.Creator != null ? i.Creator.userName : "알 수 없음"
                    }, 
                    true
                ),
            };
        }

        public async Task<InviteOutput?> GetByAsync(User? currentUser = null)
        {
            var user = await CurrentUser();
            var invite = await GetOrAllocateInviteForUser(user);
            return DTO<InviteOutput, Invite>.Convert(invite);
        }

        // ============ PRIVATE FUNC LINE ============ //

        private async Task<User?> CurrentUser() => await _currentUserService.GetUserAsync();

        private async Task<string> GenerateUniqueInviteCode(User user)
        {
            const int maxAttempts = 10;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                string code = Utils.GenerateInviteCode(user.userId);
                if (!await _context.invite.AnyAsync(i => i.inviteCode == code))
                    return code;
            }
            throw new Exception("초대 코드 생성 실패: 중복 계속 발생");
        }

        private async Task<Invite> GetOrAllocateInviteForUser(User user)
        {
            var existing = await _context.invite.FirstOrDefaultAsync(i => i.usedBy == user.userId && !i.IsUsed);
            if (existing != null)
                return existing;

            var unused = await _context.invite.Where(i => i.IsUsed == false && i.usedBy == null)
                .OrderBy(i => i.inviteId).FirstOrDefaultAsync() ??
                throw new KeyNotFoundException("여분 초대 코드 없음");

            unused.AllocTo(user);

            await _context.SaveChangesAsync();

            return unused;
        }

        private IQueryable<Invite> QueryFilter(InviteFilter filter, IQueryable<Invite> query)
        {
            if (filter.IsUsed.HasValue)
            {
                query = query.Where(i => i.IsUsed == filter.IsUsed.Value);
            }
            query = query.Where(i => i.createDate >= filter.createStartDate && i.createDate <= filter.createLastDate);

            if (!string.IsNullOrEmpty(filter.createdByName))
            {
                query = query.Where(i => i.Creator != null &&
                    EF.Functions.Like(i.Creator.userName, $"%{filter.createdByName}%"));
            }

            return query;
        }

        private IQueryable<Invite> QueryFilterFromRaw(InviteFilter filter, User user, IQueryable<Invite> query)
        {
            filter.createdByName ??= "";

            string sql = $@"
                    SELECT * FROM invite
                    WHERE 1=1
                    AND createDate BETWEEN {filter.createStartDate} AND {filter.createLastDate}
                    AND createdByName LIKE '%{filter.createdByName}%'";

            string usedSql = $"AND IsUsed = {filter.IsUsed}";


            sql = filter.IsUsed.HasValue ?
                sql + usedSql :      // IsUsed가 null 이 아니라면 IsUSed 조건 추가
                sql;                 // Null 이라면

            return _context.invite.FromSqlRaw(sql);
        }

    }
}

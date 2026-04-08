using Microsoft.EntityFrameworkCore;
using WebAPI.Context;
using WebAPI.Manager.Interfaces;
using WebAPI.Models;

namespace WebAPI.Manager
{
    public class BuildingManager : IBaseManager<Building, BuildingInput>, IGetAll<BuildingOutput, BuildingFilter>, IGetID<BuildingOutput>
    {
        private readonly AppDbContext _context;
        public BuildingManager(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Building> AddAsync(BuildingInput input, User? currentUser = null)
        {
            var user = Utils.EnsureUser(currentUser);

            input.Validate();

            await CheckExistForAdd(input);

            var building = new Building();
            building.Create(input, user.userId); 

            _context.building.Add(building);
            await _context.SaveChangesAsync();

            return building;
        }


        public async Task DeleteAsync(int id, User? currentUser = null)
        {
            // 건물 체크
            var building = await _context.building.FindAsync(id) 
                ?? throw new KeyNotFoundException("해당 건물은 존재하지 않습니다.");

            // 현재 유저 체크
            var user = Utils.EnsureUser(currentUser);

            // 권한 체크 
            building.CheckPermissionForDelete(user);

            // FK review LIST 전부 제거 후 빌딩 제거 
            var reviews = await _context.review.Where(r => r.buildingId == building.buildingId).ToListAsync();
            _context.review.RemoveRange(reviews);
            _context.building.Remove(building);

            await _context.SaveChangesAsync();
        }

        public async Task<Utils.PagedResult<BuildingOutput>> GetAllAsync(BuildingFilter filter, User? currentUser = null)
        {
            var user = Utils.EnsureUser(currentUser);

            // 전체 쿼리
            IQueryable<Building> query = _context.building;

            // 필터
            query = QueryFilter(filter, query);

            // 페이지에 출력될 데이터 추출
            return new Utils.PagedResult<BuildingOutput>
            {
                Items = await GetPageData(filter.page, query),
                TotalCount = await query.CountAsync()
            };
        }


        public async Task<BuildingOutput?> GetByIdAsync(int id, User? currentUser = null)
        {
            // 건물 체크
            var building = await _context.building.FindAsync(id);
            if (building == null)
                throw new KeyNotFoundException("해당 건물 없음");

            //if (!building.CanUpload)
            //    throw new InvalidDataException("아직 등록 되지 않음");

            // 조회한 id값 출력
            return DTO<BuildingOutput, Building>.Convert(building);
        }

        // ============ FUNC LINE ============ //

        private async Task CheckExistForAdd(BuildingInput input)
        {
            // 건물 체크
            bool exists = await _context.building.AnyAsync(b => b.buildingName == input.buildingName 
            ||  b.buildingAddress == input.buildingAddress);
            if(exists)
                throw new InvalidOperationException("이미 존재하는 건물 (이름 또는 주소) 입니다.");
        }

        

        private IQueryable<Building> QueryFilter(BuildingFilter filter, IQueryable<Building> query)
        {
            // 필터
            // query = query.Where(b => b.CanUpload);
            if (!string.IsNullOrEmpty(filter.buildingName))
                query = query.Where(b => EF.Functions.Like(b.buildingName, $"%{filter.buildingName}%"));
            if (!string.IsNullOrEmpty(filter.buildingAddress))
                query = query.Where(b => EF.Functions.Like(b.buildingAddress, $"%{filter.buildingAddress}%"));
            if (filter.minAvgRating.HasValue)
                query = query.Where(b => b.avgRating >= filter.minAvgRating.Value);
            if (filter.maxAvgRating.HasValue)
                query = query.Where(b => b.avgRating <= filter.maxAvgRating.Value);
            if (filter.minReviewCount.HasValue)
                query = query.Where(b => b.reviewCount >= filter.minReviewCount.Value);
            if (filter.maxReviewCount.HasValue)
                query = query.Where(b => b.reviewCount <= filter.maxReviewCount.Value);
            if (!string.IsNullOrEmpty(filter.uploadUserName))
            {
                query = query.Include(b => b.UploadUser).Where(b => b.UploadUser != null &&
                    EF.Functions.Like(b.UploadUser.userName, $"%{filter.uploadUserName}%"));
            }

            return query;
        }

        private async Task<List<BuildingOutput>> GetPageData(int page, IQueryable<Building> query)
        {
            // 페이징 & 정렬
            return await query.OrderByDescending(b => b.buildingId).Skip((page - 1) * Utils.pageCount).Take(Utils.pageCount)
                .Select(b => new BuildingOutput
                {
                    buildingName = b.buildingName,
                    buildingAddress = b.buildingAddress,
                    avgRating = b.avgRating,
                    reviewCount = b.reviewCount,
                    uploadUserId = b.uploadUserId
                }).ToListAsync();
        }
    }
}

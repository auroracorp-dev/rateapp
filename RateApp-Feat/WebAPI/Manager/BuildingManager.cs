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
            // 현재 유저 체크
            var user = Utils.EnsureUser(currentUser);

            // 문자열 체크
            ChekcTextForAdd(input);

            // 이미 존재하는지 체크
            await CheckExistForAdd(input);

            Building building = new Building
            {
                buildingName = input.buildingName,
                buildingAddress = input.buildingAddress,
                avgRating = 0,                         // 최초 = 0
                reviewCount = 0,                       // 최초 = 0
                uploadUserId = user.userId,
                CanUpload = false,                     // 추후 관리자가 true로 바꿔야 표시되어짐 => 이거 필요없을것 같음
            };

            // 새로운 건물 등록
            _context.building.Add(building);

            // 저장
            await _context.SaveChangesAsync();

            return building;
        }

        public async Task DeleteAsync(int id, User? currentUser = null)
        {
            // 건물 체크
            var building = await _context.building.FindAsync(id);
            if (building == null)
                throw new KeyNotFoundException("해당 건물은 존재하지 않습니다.");

            // 현재 유저 체크
            var user = Utils.EnsureUser(currentUser);

            CheckPermissionForDelete(building, user);

            // ★★★★★ 관련 외래키 다 지우고 지워야될것 같다.

            // 리뷰 제거
            _context.building.Remove(building);

            await _context.SaveChangesAsync();
        }

        public async Task<Utils.PagedResult<BuildingOutput>> GetAllAsync(BuildingFilter filter, int page = 1, User? currentUser = null)
        {
            var user = Utils.EnsureUser(currentUser);

            // 전체 쿼리
            IQueryable<Building> query = _context.building;

            // 필터
            query = QueryFilter(filter, query);

            // 페이지에 출력될 데이터 추출
            var totalCount = await query.CountAsync();
            List<BuildingOutput> items = await GetPageData(page, query);

            return new Utils.PagedResult<BuildingOutput>
            {
                Items = items,
                TotalCount = totalCount
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
            var oldBuildingName = await _context.building.FirstOrDefaultAsync(b => b.buildingName == input.buildingName);
            var oldBuildingAddress = await _context.building.FirstOrDefaultAsync(b => b.buildingAddress == input.buildingAddress);
            if (oldBuildingName != null || oldBuildingAddress != null)
                throw new InvalidOperationException("이미 존재하는 건물 (이름 또는 주소) 입니다.");
        }

        private void ChekcTextForAdd(BuildingInput input)
        {
            // 문자열 체크
            bool checkName = TextFilter.ContainsBadWord(input.buildingName);
            bool checkAdd = TextFilter.ContainsBadWord(input.buildingAddress);
            if (checkName || checkAdd)
                throw new InvalidOperationException("잘못된 입력입니다.");
        }

        private void CheckPermissionForDelete(Building building, User user)
        {
            // 권한 체크
            bool isManager = Utils.HasManagerPermission(user.role);
            bool isOwner = user.userId == building.uploadUserId;
            if (!isManager && !isOwner)
                throw new UnauthorizedAccessException("권한 없음");
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
            var items = await query.OrderByDescending(b => b.buildingId) // 최신 등록 순
                .Skip((page - 1) * Utils.pageCount).Take(Utils.pageCount)
                .Select(b => new BuildingOutput
                {
                    buildingName = b.buildingName,
                    buildingAddress = b.buildingAddress,
                    avgRating = b.avgRating,
                    reviewCount = b.reviewCount,
                    uploadUserId = b.uploadUserId
                })
                .ToListAsync();
            return items;
        }

    }
}

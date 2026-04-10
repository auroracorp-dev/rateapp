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

        // ============ PUBLIC FUNC LINE ============ //

        public async Task<Building> AddAsync(BuildingInput input, User? currentUser = null)
        {
            //var user = Utils.EnsureUser(currentUser);

            input.Validate();

            await CheckExistForAdd(input);

            var building = new Building(input, currentUser);

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
            // var user = Utils.EnsureUser(currentUser);

            // 권한 체크 
            // building.CheckPermissionForDelete(currentUser);

            // FK review LIST 전부 제거 후 빌딩 제거 
            var reviews = await _context.review.Where(r => r.buildingId == building.buildingId).ToListAsync();
            _context.review.RemoveRange(reviews);
            _context.building.Remove(building);

            await _context.SaveChangesAsync();
        }
        public async Task<Utils.PagedResult<BuildingOutput>> GetAllAsync(BuildingFilter filter, User? currentUser = null)
        {
            // var user = Utils.EnsureUser(currentUser);

            // 전체 쿼리
            IQueryable<Building> query = _context.building;

            // 필터
            query = QueryFilter(filter, query);

            // 페이지에 출력될 데이터 추출
            return new Utils.PagedResult<BuildingOutput>
            {
                Items = await PagingHelper.GetPageData<Building, BuildingOutput, int>(
                        query: query, page: filter.page,
                        orderBy: b => b.buildingId,
                        selector: b => new BuildingOutput
                        {
                            buildingName = b.buildingName,
                            buildingAddress = b.buildingAddress,
                            avgRating = b.avgRating,
                            reviewCount = b.reviewCount,
                            uploadUserId = b.uploadUserId
                        }
                    ),
                TotalCount = await query.CountAsync()
            };
        }
        public async Task<BuildingOutput?> GetByIdAsync(int id, User? currentUser = null)
        {
            // 건물 체크
            var building = await _context.building.FindAsync(id) ??
                throw new KeyNotFoundException("해당 건물은 존재하지 않습니다.");

            // 조회한 id값 출력
            return DTO<BuildingOutput, Building>.Convert(building);
        }

        // ============ PRIVATE FUNC LINE ============ //

        private async Task CheckExistForAdd(BuildingInput input)
        {
            // 건물 체크
            bool exists = await _context.building.AnyAsync(b =>
                (b.buildingName == input.buildingName.ToLower()) || (b.buildingAddress == input.buildingAddress.ToLower()));
            if (exists)
                throw new InvalidOperationException("이미 존재하는 건물 (이름 또는 주소) 입니다.");
        }

        private IQueryable<Building> QueryFilter(BuildingFilter filter, IQueryable<Building> query)
        {
            if (!string.IsNullOrEmpty(filter.buildingName))
                query = query.Where(b => EF.Functions.Like(b.buildingName, $"%{filter.buildingName}%"));

            if (!string.IsNullOrEmpty(filter.buildingAddress))
                query = query.Where(b => EF.Functions.Like(b.buildingAddress, $"%{filter.buildingAddress}%"));

            query = query.Where(b => b.avgRating >= filter.minAvgRating && b.avgRating <= filter.maxAvgRating);
            query = query.Where(b => b.reviewCount >= filter.minReviewCount && b.reviewCount <= filter.maxReviewCount);

            if (!string.IsNullOrEmpty(filter.uploadUserName))
                query = query.Where(b => b.UploadUser != null &&
                    EF.Functions.Like(b.UploadUser.userName, $"%{filter.uploadUserName}%"));

            return FilterToRange(filter, query);
        }
        // Query 버전
        private IQueryable<Building> QueryFilterFromRaw(BuildingFilter filter)
        {
            // null 값을 빈 문자열로 초기화
            filter.buildingName ??= "";
            filter.buildingAddress ??= "";
            filter.uploadUserName ??= "";

            const double Degree = 111.0;

            double lat = filter.curLat;
            double lng = filter.curLng;
            double range = filter.range;

            double latRange = range / Degree;
            double lngRange = range / (Degree * Math.Cos(lat * Math.PI / 180));
            double rangeSquared = Math.Pow(range / Degree, 2);

            // FromSqlRaw + 모든 필터 + 거리 범위 + 거리 순 정렬
            return _context.building.FromSqlRaw($@"
                SELECT b.* FROM building AS b
                LEFT JOIN user AS u ON b.uploadUserId = u.userId
                WHERE 1=1
                  AND b.buildingName LIKE '%{filter.buildingName}%'
                  AND b.buildingAddress LIKE '%{filter.buildingAddress}%'
                  AND b.avgRating BETWEEN {filter.minAvgRating} AND {filter.maxAvgRating}
                  AND b.reviewCount BETWEEN {filter.minReviewCount} AND {filter.maxReviewCount}
                  AND u.userName LIKE '%{filter.uploadUserName}%'
                  AND b.latitude BETWEEN {lat - latRange} AND {lat + latRange}
                  AND b.longitude BETWEEN {lng - lngRange} AND {lng + lngRange}
                  AND POWER(b.latitude - {lat}, 2) + POWER(b.longitude - {lng}, 2) <= {rangeSquared}
                ORDER BY POWER(b.latitude - {lat}, 2) + POWER(b.longitude - {lng}, 2)
            ");
        }
        private IQueryable<Building> FilterToRange(BuildingFilter filter, IQueryable<Building> query)
        {
            const double Degree = 111.0;

            double lat = filter.curLat;
            double lng = filter.curLng;
            double range = filter.range;

            // 바운더리 체크
            double latRange = range / Degree;
            double lngRange = range / (Degree * Math.Cos(lat * Math.PI / 180));
            query = query.Where(b => b.latitude >= lat - latRange && b.latitude <= lat + latRange &&
                b.longitude >= lng - lngRange && b.longitude <= lng + lngRange
            );

            // 범위 체크
            double rangeSquared = Math.Pow(range / Degree, 2);

            query = query.Where(b => Math.Pow(b.latitude - lat, 2) + Math.Pow(b.longitude - lng, 2)
                <= rangeSquared
            );

            // 거리 순 정렬
            query = query.OrderBy(b => Math.Pow(b.latitude - lat, 2) + Math.Pow(b.longitude - lng, 2)
);
            return query;
        }
    }
}

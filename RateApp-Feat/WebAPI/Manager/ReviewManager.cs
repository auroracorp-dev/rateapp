using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using WebAPI.Context;
using WebAPI.Manager.Interfaces;
using WebAPI.Models;

namespace WebAPI.Manager
{
    public class ReviewManager : IBaseManager<Review, ReviewInput>, IGetAll<ReviewOutput, ReviewFilter>, IGetID<ReviewOutput>, IUpdate<ReviewOutput, ReviewUpdateInput>
    {
        private readonly AppDbContext _context;
        private readonly CurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public ReviewManager(AppDbContext context, CurrentUserService currentUserService, IMapper mapper)
        {
            _context = context;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        // ============ PUBLIC FUNC LINE ============ //

        public async Task<Review> AddAsync(ReviewInput input)
        {
            var user = await CurrentUser() ??
                throw new Exception("User 없음");

            var building = await _context.building.FindAsync(input.buildingId) ??
                throw new KeyNotFoundException("해당 건물이 존재하지 않습니다.");

            var review = await _context.review.FirstOrDefaultAsync
                (r => r.userId == user.userId && r.buildingId == input.buildingId);
            // 업데이트
            if (review != null)
            {
                UpdateExistingReview(review, input, building);
            }
            // 새로 생성
            else
            {
                review = CreateNewReview(input, user, building);
            }
            await _context.SaveChangesAsync();
            return review;
        }


        public async Task DeleteAsync(int id)
        {
            var user = await CurrentUser() ??
                throw new Exception("USER IS NULL _ DELETE ASYNC _ REVIEW");

            var review = await _context.review.FindAsync(id) ??
                throw new KeyNotFoundException("해당 리뷰가 존재하지 않습니다.");
            var building = await _context.building.FindAsync(review.buildingId) ??
                throw new KeyNotFoundException("해당 건물이 존재하지 않습니다.");
            review.CheckPermissionForDelete(user);

            // 건물 리뷰 제거 우선 반영
            building.RemoveReview(review.rating);
            // 리뷰 제거
            _context.review.Remove(review);

            await _context.SaveChangesAsync();
        }

        public async Task<Utils.PagedResult<ReviewOutput>> GetAllAsync(ReviewFilter filter)
        {
            // 쿼리 전체
            IQueryable<Review> query = _context.review.AsNoTracking();
            query = QueryFilter(filter, query);

            return new Utils.PagedResult<ReviewOutput>
            {
                Items = await PagingHelper.GetPageData(
                    query: query, page: filter.page,
                    orderBy: r => r.createDate,
                    selector: r => new ReviewOutput
                    {
                        reviewId = r.reviewId,
                        buildingId = r.buildingId,
                        userId = r.userId,
                        likeCount = r.likeCount,
                        rating = r.rating,
                        comment = r.comment
                    }
                ),
                TotalCount = await query.CountAsync()
            };
        }

        public async Task<ReviewOutput?> GetByIdAsync(int id)
        {
            var review = await _context.review.AsNoTracking().Include(r => r.Building).
                FirstOrDefaultAsync(r => r.reviewId == id) ??
                throw new KeyNotFoundException("해당 리뷰 없음");
            if (review.Building != null)
                throw new InvalidDataException("아직 등록 되지 않음");

            return DTO<ReviewOutput, Review>.Convert(review);
        }
        public async Task<ReviewOutput?> UpdateAsync(int id, ReviewUpdateInput updateInput)
        {
            updateInput.Validate();
            // 유저 체크
            var user = await CurrentUser() ??
                throw new Exception("USER IS NULL_UPDATE_ASYNC_REVIEW");
            // 해당 건물 정보 취득
            var target = await _context.review.FindAsync(id) ??
                throw new KeyNotFoundException("해당 리뷰는 존재하지 않습니다).");
            // 자기자신이 작성한 글 || 관리자 권한
            if (!user.IsMine(target.userId) && !user.HasManagerPermission())
                throw new Exception("권한이 없습니다.");

            // aft_buildingInput의 프로퍼티에서 값이 있는 애만 수정하고 없는 애는 bef_의 값 그대로
            // 사용하게 하고 싶다.
            _mapper.Map(updateInput, target);

            await _context.SaveChangesAsync();

            return _mapper.Map<ReviewOutput>(target);
        }

        // ============ PRIVATE FUNC LINE ============ //
        private async Task<User?> CurrentUser() => await _currentUserService.GetUserAsync();

        private void UpdateExistingReview(Review review, ReviewInput input, Building building)
        {
            var originRating = review.rating;
            review.UpdateReview(input);
            building.UpdateReview(originRating, review.rating);
        }
        private Review CreateNewReview(ReviewInput input, User user, Building building)
        {
            var review = new Review(input, user);
            _context.review.Add(review);
            building.AddNewReview(input.rating);
            return review;
        }

        private IQueryable<Review> QueryFilter(ReviewFilter filter, IQueryable<Review> query)
        {
            // 필터
            query = query.Where(r => r.Building != null /* && r.Building.CanUpload*/);
            if (!string.IsNullOrEmpty(filter.buildingName))
                query = query.Where(r => r.Building != null && EF.Functions.Like(r.Building.buildingName, $"%{filter.buildingName}%"));
            if (!string.IsNullOrEmpty(filter.userName))
                query = query.Where(r => r.User != null && EF.Functions.Like(r.User.userName, $"%{filter.userName}%"));
            query = query.Where(r => r.likeCount >= filter.MinLikeCount && r.likeCount <= filter.MaxLikeCount);
            query = query.Where(r => r.rating >= filter.MinRating && r.rating <= filter.MaxRating);
            if (!string.IsNullOrEmpty(filter.comment))
                query = query.Where(r => r.comment != null && EF.Functions.Like(r.comment, $"%{filter.comment}%"));
            return query;
        }
        private IQueryable<Review> QueryFilterFromRaw (ReviewFilter filter, IQueryable<Review> query)
        {
            filter.buildingName ??= "";
            filter.userName ??= "";
            filter.comment ??= "";

            return _context.review.FromSqlRaw($@"
                    SELECT * FROM review AS r
                    LEFT JOIN user AS u ON r.userId = u.userId
                    LEFT JOIN building AS b ON r.buildingId = b.buildingId
                    WHERE 1=1
                      AND r.buildingName LIKE '%{filter.buildingName}%'
                      AND r.comment LIKE '%{filter.comment}%'
                      AND u.userName LIKE '%{filter.userName}%'
                      AND r.likeCount BETWEEN {filter.MinLikeCount} AND {filter.MaxLikeCount}
                      AND r.rating BETWEEN {filter.MinRating} AND {filter.MaxRating}
            ");
        }
    }
}

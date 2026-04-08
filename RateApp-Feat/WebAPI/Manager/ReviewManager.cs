using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using WebAPI.Context;
using WebAPI.Manager.Interfaces;
using WebAPI.Models;

namespace WebAPI.Manager
{
    public class ReviewManager : IBaseManager<Review, ReviewInput>, IGetAll<ReviewOutput, ReviewFilter>, IGetID<ReviewOutput>
    {
        private readonly AppDbContext _context;
        public ReviewManager(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Review> AddAsync(ReviewInput input, User? currentUser = null)
        {
            // 문자열 체크
            bool checkCommnet = TextFilter.ContainsBadWord(input.comment);
            if (checkCommnet)
                throw new InvalidOperationException("잘못된 입력입니다.");

            // 유저 체크
            var user = Utils.EnsureUser(currentUser);

            // 건물 존재 여부 체크
            var building = await _context.building.FindAsync(input.buildingId)
                ?? throw new KeyNotFoundException("해당 건물이 존재하지 않습니다.");

            // 기존 리뷰 조회 (유저 + 건물 기준)
            var existingReview = await _context.review
                .FirstOrDefaultAsync(r => r.userId == user.userId && r.buildingId == input.buildingId);

            // 이미 리뷰가 있다면 → 수정 처리
            if (existingReview != null)
            {
                var originRating = existingReview.rating;
                existingReview.rating = input.rating;
                existingReview.comment = input.comment;

                building.UpdateReview(originRating, existingReview.rating);
                await _context.SaveChangesAsync();
                return existingReview;
            }
            // 신규 리뷰 생성
            else
            {
                var newReview = new Review
                {
                    buildingId = input.buildingId,
                    userId = user.userId,
                    likeCount = 0,
                    rating = input.rating,
                    comment = input.comment,
                    createDate = DateTime.UtcNow,
                };

                _context.review.Add(newReview);

                // 건물 평점 반영
                building.AddNewReview(input.rating);

                // 저장
                await _context.SaveChangesAsync();

                return newReview;
            }
        }

        public async Task DeleteAsync(int id, User? currentUser = null)
        {
            var review = await _context.review.FindAsync(id);
            if (review == null)
                throw new KeyNotFoundException("해당 리뷰가 존재하지 않습니다.");
            // 건물 체크
            var building = await _context.building.FindAsync(review.buildingId);
            if (building == null)
                throw new KeyNotFoundException("해당 건물이 존재하지 않습니다.");

            // 현재 유저 체크
            var user = Utils.EnsureUser(currentUser);
            // 권한 체크
            bool isManager = Utils.HasManagerPermission(user.role);
            bool isOwner = user.userId == review.userId;
            if (!isManager && !isOwner)
                throw new UnauthorizedAccessException("권한 없음");

            // 건물 리뷰 제거 우선 반영
            building.RemoveReview(review.rating);
            // 리뷰 제거
            _context.review.Remove(review);

            await _context.SaveChangesAsync();
        }

        public async Task<Utils.PagedResult<ReviewOutput>> GetAllAsync(ReviewFilter filter, User? currentUser = null)
        {
            // 유저 체크
            var user = Utils.EnsureUser(currentUser);

            // 쿼리필터
            IQueryable<Review> query = QueryFilter(filter);

            // 전체 쿼리
            var totalCount = await query.CountAsync();
            var items = await query.OrderByDescending(r => r.createDate).Skip((filter.page - 1) * Utils.pageCount).Take(Utils.pageCount)
                .Select(r => new ReviewOutput
                {
                    reviewId = r.reviewId,
                    buildingId = r.buildingId,
                    userId = r.userId,
                    likeCount = r.likeCount,
                    rating = r.rating,
                    comment = r.comment
                }).ToListAsync();

            return new Utils.PagedResult<ReviewOutput>
            {
                Items = items,
                TotalCount = totalCount
            };
        }

        private IQueryable<Review> QueryFilter(ReviewFilter filter)
        {
            // 쿼리 전체
            IQueryable<Review> query = _context.review.Include(r => r.User).Include(r => r.Building);
            // 필터
            query = query.Where(r => r.Building != null /* && r.Building.CanUpload*/);
            if (!string.IsNullOrEmpty(filter.buildingName))
                query = query.Where(r => r.Building != null && EF.Functions.Like(r.Building.buildingName, $"%{filter.buildingName}%"));
            if (!string.IsNullOrEmpty(filter.userName))
                query = query.Where(r => r.User != null && EF.Functions.Like(r.User.userName, $"%{filter.userName}%"));
            if (filter.MinLikeCount.HasValue)
                query = query.Where(r => r.likeCount >= filter.MinLikeCount.Value);
            if (filter.MaxLikeCount.HasValue)
                query = query.Where(r => r.likeCount <= filter.MaxLikeCount.Value);
            if (filter.MinRating.HasValue)
                query = query.Where(r => r.rating >= filter.MinRating.Value);
            if (filter.MaxRating.HasValue)
                query = query.Where(r => r.rating <= filter.MaxRating.Value);
            if (!string.IsNullOrEmpty(filter.comment))
                query = query.Where(r => r.comment != null && EF.Functions.Like(r.comment, $"%{filter.comment}%"));
            return query;
        }

        public async Task<ReviewOutput?> GetByIdAsync(int id, User? currentUser = null)
        {
            var review = await _context.review.FindAsync(id);
            if (review == null)
                throw new KeyNotFoundException("해당 리뷰 없음");

            if (review.Building != null /*&& !review.Building.CanUpload*/)
                throw new InvalidDataException("아직 등록 되지 않음");

            // 조회한 id값 출력
            return DTO<ReviewOutput, Review>.Convert(review);
        }
    }
}

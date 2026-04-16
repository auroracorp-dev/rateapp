using Microsoft.EntityFrameworkCore;
using WebAPI.Context;
using WebAPI.Models;

namespace WebAPI.Manager
{
    public class LikeManager
    {
        private readonly AppDbContext _context;
        private readonly CurrentUserService _currentUserService;

        public LikeManager(AppDbContext context, CurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        // ============ PUBLIC FUNC LINE ============ //

        public async Task<LikeToggleResult> ToggleAsync(LikeInput input, User? currentUser = null)
        {
            var user = await CurrentUser() ??
                throw new Exception("USER IS NULL _ TOGGLE ASYNC");

            var review = await _context.review.FindAsync(input.reviewId) ??
                throw new KeyNotFoundException("리뷰가 존재하지 않습니다.");

            var existingLike = await _context.review_like.FirstOrDefaultAsync
                (x => x.reviewId == input.reviewId && x.userId == user.userId);

            bool liked;

            if (existingLike != null)
            {
                _context.review_like.Remove(existingLike);
                review.RemoveLike();
                liked = false;
            }
            else
            {
                _context.review_like.Add(new Like
                {
                    reviewId = input.reviewId,
                    userId = user.userId
                });

                review.AddLike();
                liked = true;
            }

            await _context.SaveChangesAsync();

            return new LikeToggleResult {
                liked = liked,
                reviewId = review.reviewId,
                userId = user.userId,
                likeCount = review.likeCount
            };
        }

        // ============ PRIVATE FUNC LINE ============ //

        private async Task<User?> CurrentUser() => await _currentUserService.GetUserAsync();

    }
}
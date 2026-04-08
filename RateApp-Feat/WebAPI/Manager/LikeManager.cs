using Microsoft.EntityFrameworkCore;
using WebAPI.Context;
using WebAPI.Models;

namespace WebAPI.Manager
{
    public class LikeManager
    {
        private readonly AppDbContext _context;

        public LikeManager(AppDbContext context)
        {
            _context = context;
        }

        public async Task<LikeToggleResult> ToggleAsync(LikeInput input, User? currentUser = null)
        {
            var user = Utils.EnsureUser(currentUser);

            var review = await _context.review.FindAsync(input.reviewId);
            if (review == null)
                throw new KeyNotFoundException("리뷰가 존재하지 않습니다.");

            var existingLike = await _context.review_like
                .FirstOrDefaultAsync(x => x.reviewId == input.reviewId && x.userId == user.userId);

            if (existingLike != null)
            {
                if (!review.RemoveLike())
                    throw new Exception("좋아요 수가 0보다 작아질 수 없습니다.");

                _context.review_like.Remove(existingLike);

                await _context.SaveChangesAsync();

                return new LikeToggleResult
                {
                    liked = false,
                    reviewId = review.reviewId,
                    userId = user.userId,
                    likeCount = review.likeCount
                };
            }

            var newLike = new Like
            {
                reviewId = input.reviewId,
                userId = user.userId
            };

            _context.review_like.Add(newLike);
            review.AddLike();

            await _context.SaveChangesAsync();

            return new LikeToggleResult
            {
                liked = true,
                reviewId = review.reviewId,
                userId = user.userId,
                likeCount = review.likeCount
            };
        }
    }
}
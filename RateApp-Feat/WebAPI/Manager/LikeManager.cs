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
            // var user = Utils.EnsureUser(currentUser);

            var review = await _context.review.FindAsync(input.reviewId)
                ?? throw new KeyNotFoundException("리뷰가 존재하지 않습니다.");

            var existingLike = await _context.review_like
                .FirstOrDefaultAsync(x => x.reviewId == input.reviewId && x.userId == currentUser.userId);

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
                    userId = currentUser.userId
                });

                review.AddLike();
                liked = true;
            }

            await _context.SaveChangesAsync();

            return new LikeToggleResult {
                liked = liked,
                reviewId = review.reviewId,
                userId = currentUser.userId,
                likeCount = review.likeCount
            };
        }
    }
}
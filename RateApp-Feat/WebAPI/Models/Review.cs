using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAPI.Models;
public class Review
{
    public Review() { }
    public Review(ReviewInput input, User user)
    {
        this.buildingId = input.buildingId;
        this.userId = user.userId;
        this.likeCount = 0;
        this.rating = input.rating;
        this.comment = input.comment;
        this.createDate = DateTime.UtcNow;
    }

    // ============ VAR LINE ============ //

    public int reviewId { get; set; }
    public int buildingId { get; set; }
    public int userId { get; set; }
    public int likeCount { get; set; }
    public int rating { get; set; }
    public string? comment { get; set; }
    public DateTime createDate { get; set; } = DateTime.UtcNow;

    // ============ FK LINE ============ //

    public virtual Building? Building { get; set; }
    public virtual User? User { get; set; }

    // ============ FUNC LINE ============ //
    public void AddLike()
    {
        this.likeCount++;
    }
    public bool RemoveLike()
    {
        if (this.likeCount <= 0)
            return false;

        this.likeCount--;
        return true;
    }
    public void UpdateReview(ReviewInput input)
    {
        this.rating = input.rating;
        this.comment = input.comment;
    }
    public void CheckPermissionForDelete(User user)
    {
        // 권한 체크
        bool isManager = Utils.HasManagerPermission(user.role);
        bool isOwner = user.userId == this.userId;
        if (!isManager && !isOwner)
            throw new UnauthorizedAccessException("권한 없음");
    }
}

public class ReviewInput
{
    [Required] public int buildingId { get; set; } = 0;
    [Required]
    [Range(0, 10, ErrorMessage = "별점은 0에서 10 사이여야 합니다.")]
    public int rating { get; set; } = 0;
    [Required]
    [MinLength(6)]
    [MaxLength(50)]
    private string _comment;
    public string comment
    {
        get => _comment;
        set => _comment = value?.Trim();
    }
    public void Validate()
    {
        if (TextFilter.ContainsBadWord(comment))
            throw new InvalidOperationException("잘못된 입력입니다.");
    }
}

public class ReviewOutput
{
    public int reviewId { get; set; } = 0;
    public int buildingId { get; set; } = 0;
    public int userId { get; set; } = 0;
    public int likeCount { get; set; } = 0;
    public int rating { get; set; } = 0;
    public string? comment { get; set; } = null;
}

public class ReviewFilter
{
    public int page { get; set; } = 1;
    public string? buildingName { get; set; }
    public string? userName { get; set; }

    [Range(0, UInt16.MaxValue)] public int MinLikeCount { get; set; } = 0;
    [Range(0, UInt16.MaxValue)] public int MaxLikeCount { get; set; } = UInt16.MaxValue;

    [Range(0, Utils.pageCount)] public int MinRating { get; set; } = 0;
    [Range(0, Utils.pageCount)] public int MaxRating { get; set; } = Utils.pageCount;

    public string? comment { get; set; }
}

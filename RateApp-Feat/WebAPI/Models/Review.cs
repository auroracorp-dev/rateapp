using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAPI.Models;
public class Review
{
    public int reviewId { get; set; }
    public int buildingId { get; set; }
    public int userId { get; set; }
    public int likeCount { get; set; }
    public int rating { get; set; }
    public string? comment { get; set; }
    public DateTime createDate { get; set; } = DateTime.UtcNow;

    public virtual Building? Building { get; set; }
    public virtual User? User { get; set; }
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
        set => _comment = value?.Trim().ToLower();
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

    public int? MinLikeCount { get; set; }
    public int? MaxLikeCount { get; set; }

    public int? MinRating { get; set; }
    public int? MaxRating { get; set; }

    public string? comment { get; set; }
}

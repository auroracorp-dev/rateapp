using System.ComponentModel.DataAnnotations.Schema;

namespace WebAPI.Models;
public class Like
{
    public int reviewId { get; set; } = 0;
    public int userId { get; set; } = 0;
    [ForeignKey("reviewId")]
    public virtual Review? Review { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }
}
public class LikeInput
{
    public int reviewId { get; set; } = 0;
}
public class LikeOutput
{
    public int userId { get; set; } = 0;
    public int reviewId { get; set; } = 0;
}
public class LikeToggleResult
{
    public bool liked { get; set; }
    public int reviewId { get; set; }
    public int userId { get; set; }
    public int likeCount { get; set; }
}

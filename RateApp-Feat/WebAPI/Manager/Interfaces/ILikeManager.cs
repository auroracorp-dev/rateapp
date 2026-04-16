using WebAPI.Models;
namespace WebAPI.Manager;

public interface ILikeManager
{
    Task<LikeToggleResult> ToggleAsync(LikeInput input);
}

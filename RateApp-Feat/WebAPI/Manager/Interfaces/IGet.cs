using WebAPI.Models;

namespace WebAPI.Manager.Interfaces
{
    public interface IGet<TOutput>
    {
        // 조회
        Task<TOutput?> GetByAsync(User? currentUser = null);

    }
}

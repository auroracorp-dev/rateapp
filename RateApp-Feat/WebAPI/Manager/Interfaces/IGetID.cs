using WebAPI.Models;

namespace WebAPI.Manager.Interfaces
{
    public interface IGetID <TOutput>
    {
        // 조회
        Task<TOutput?> GetByIdAsync(int id, User? currentUser = null);

    }
}

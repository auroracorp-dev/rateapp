using WebAPI.Models;
using static Utils;

namespace WebAPI.Manager.Interfaces
{
    public interface IGetAll<TOutput, TFilter>
    {
        // 페이지 1부터이지만 프론트에서 index 0부터 넘기면 할당받은 모든 값 1에서 0로 바꾸고 -1 지워야함.
        // 전체 조회
        Task<PagedResult<TOutput>> GetAllAsync(TFilter filter, User? currentUser = null);
    }
}

using Microsoft.AspNetCore.Mvc;
using WebAPI.Models;

namespace WebAPI.Manager.Interfaces
{
    public interface IBaseManager<TEntity, TInput>
    {
        // 추가
        Task<TEntity> AddAsync(TInput input);

        // 삭제
        Task DeleteAsync(int id);
    }

}

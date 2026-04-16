using WebAPI.Models;

namespace WebAPI.Manager.Interfaces;

public interface IUpdate <Torg, Tupdate>
{
    Task<Torg?> UpdateAsync(int id, Tupdate updateInput);
}

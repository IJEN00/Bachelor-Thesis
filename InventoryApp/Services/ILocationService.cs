using InventoryApp.Models;

namespace InventoryApp.Services
{
    public interface ILocationService
    {
        Task<IEnumerable<Location>> GetAllAsync();
        Task<Location?> GetByIdAsync(int id);
        Task AddAsync(Location component);
        Task DeleteAsync(int id);
    }
}

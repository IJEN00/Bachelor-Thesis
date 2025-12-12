using InventoryApp.Models;

namespace InventoryApp.Services
{
    public interface IComponentService
    {
        Task<List<Component>> GetAllAsync();
        Task<Component?> GetByIdAsync(int id);
        Task AddAsync(Component component);
        Task UpdateAsync(Component component);
        Task DeleteAsync(int id);
        Task<List<Component>> GetLowStockAsync();
        Task<int> GetTotalComponentsAsync();
        Task<int> GetTotalQuantityAsync();
    }
}

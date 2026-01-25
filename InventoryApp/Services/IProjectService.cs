using InventoryApp.Models;

namespace InventoryApp.Services
{
    public interface IProjectService
    {
        Task<List<Project>> GetAllAsync();
        Task<Project?> GetByIdAsync(int id);
        Task<Project?> GetDetailsAsync(int id); 
        Task CreateAsync(Project project);
        Task UpdateAsync(Project project);
        Task DeleteAsync(int id);
        bool ProjectExists(int id);

        Task AddItemAsync(int projectId, int? componentId, string? customName, int quantity);
        Task DeleteItemAsync(int itemId);

        Task FindOffersAsync(int projectId); 
        Task SelectOfferAsync(int offerId);  
        Task AutoSelectCheapestAsync(int projectId); 

        Task ConsumeStockAsync(int projectId); 
        Task<byte[]> GenerateOrderCsvAsync(int projectId);
    }
}

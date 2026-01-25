using InventoryApp.Models;

namespace InventoryApp.Services
{
    public interface IDocumentService
    {
        Task<IEnumerable<Document>> GetByIdAsync(int id);
        Task AddAsync(Document component);
        Task DeleteAsync(int id);
        Task UploadFilesAsync(List<IFormFile> files, int componentId, string webRootPath);
    }
}

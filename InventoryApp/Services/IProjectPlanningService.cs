using InventoryApp.Models;

namespace InventoryApp.Services
{
    public interface IProjectPlanningService
    {
        void RecalculateInMemory(Project project);
        Task RecalculateAndSaveAsync(int projectId);
    }
}

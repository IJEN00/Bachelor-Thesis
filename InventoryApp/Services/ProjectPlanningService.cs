using InventoryApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Services
{
    public class ProjectPlanningService : IProjectPlanningService
    {
        private readonly AppDbContext _context;

        public ProjectPlanningService(AppDbContext context)
        {
            _context = context;
        }

        public async Task RecalculateAndSaveAsync(int projectId)
        {
            var project = await _context.Projects
                .Include(p => p.Items)
                    .ThenInclude(i => i.Component)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
            {
                throw new InvalidOperationException($"Project {projectId} not found");
            }

            if (project.ConsumedAt != null) return;

            RecalculateInMemory(project);

            await _context.SaveChangesAsync();
        }

        public void RecalculateInMemory(Project project)
        {
            var groupsByComponent = project.Items
                .Where(i => i.ComponentId != null && i.Component != null)
                .GroupBy(i => i.ComponentId);

            foreach (var group in groupsByComponent)
            {
                var firstItem = group.First();
                var component = firstItem.Component!;

                int available = component.Quantity;

                foreach (var item in group.OrderBy(i => i.Id))
                {
                    var required = item.QuantityRequired;

                    var fromStock = Math.Min(required, available);
                    var toBuy = required - fromStock;

                    item.QuantityFromStock = fromStock;
                    item.QuantityToBuy = toBuy;

                    available -= fromStock;
                }
            }

            foreach (var item in project.Items.Where(i => i.ComponentId == null))
            {
                item.QuantityFromStock = 0;
                item.QuantityToBuy = item.QuantityRequired;
            }
        }
    }
}

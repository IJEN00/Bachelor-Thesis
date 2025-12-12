using InventoryApp.Models;
using InventoryApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Controllers
{
    public class HomeViewModel
    {
        public int TotalComponents { get; set; }
        public int TotalQuantity { get; set; }
        public int LowStockCount { get; set; }
        public List<InventoryApp.Models.Component> LowStock { get; set; } = new();

        public List<string> ConsumptionLabels { get; set; } = new();
        public List<int> ConsumptionValues { get; set; } = new();
    }

    public class HomeController : Controller
    {
        private readonly IComponentService _components;
        private readonly AppDbContext _context;

        public HomeController(IComponentService components, AppDbContext context)
        {
            _components = components;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var low = await _components.GetLowStockAsync();

            var since = DateTime.UtcNow.AddDays(-30);

            var consumption = await _context.InventoryTransactions
                .Where(t => t.CreatedAt >= since && t.DeltaQuantity < 0)
                .GroupBy(t => t.ComponentId)
                .Select(g => new
                {
                    ComponentId = g.Key,
                    Used = -g.Sum(x => x.DeltaQuantity)
                })
                .OrderByDescending(x => x.Used)
                .Take(7)
                .ToListAsync();

            var componentIds = consumption.Select(x => x.ComponentId).ToList();

            var components = await _context.Components
                .Where(c => componentIds.Contains(c.Id))
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            var labels = new List<string>();
            var values = new List<int>();

            foreach (var c in consumption)
            {
                var name = components.FirstOrDefault(x => x.Id == c.ComponentId)?.Name ?? "Unknown";
                labels.Add(name);
                values.Add(c.Used);
            }

            var model = new HomeViewModel
            {
                TotalComponents = await _components.GetTotalComponentsAsync(),
                TotalQuantity = await _components.GetTotalQuantityAsync(),
                LowStockCount = low.Count,
                LowStock = low.Take(10).ToList(),

                ConsumptionLabels = labels,
                ConsumptionValues = values
            };

            return View(model);
        }

        public IActionResult Error() => View();
    }
}

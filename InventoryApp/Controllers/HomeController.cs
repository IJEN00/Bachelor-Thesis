using InventoryApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApp.Controllers
{
    public class HomeViewModel
    {
        public int TotalComponents { get; set; }
        public int TotalQuantity { get; set; }
        public int LowStockCount { get; set; }
        public List<InventoryApp.Models.Component> LowStock { get; set; } = new();
    }

    public class HomeController : Controller
    {
        private readonly IComponentService _components;
        public HomeController(IComponentService components) => _components = components;

        public async Task<IActionResult> Index()
        {
            var low = await _components.GetLowStockAsync();

            var model = new HomeViewModel
            {
                TotalComponents = await _components.GetTotalComponentsAsync(),
                TotalQuantity = await _components.GetTotalQuantityAsync(),
                LowStockCount = low.Count,
                LowStock = low.Take(10).ToList()
            };

            return View(model);
        }

        public IActionResult Error() => View();
    }
}

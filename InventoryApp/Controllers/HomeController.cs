using InventoryApp.Models;
using InventoryApp.Services;
using InventoryApp.ViewModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IComponentService _components;

        public HomeController(IComponentService components)
        {
            _components = components;
        }

        public async Task<IActionResult> Index()
        {
            var low = await _components.GetLowStockAsync();

            var stats = await _components.GetConsumptionStatsAsync(30);

            var model = new HomeViewModel
            {
                TotalComponents = await _components.GetTotalComponentsAsync(),
                TotalQuantity = await _components.GetTotalQuantityAsync(),
                LowStockCount = low.Count,
                LowStock = low.Take(10).ToList(),

                ConsumptionLabels = stats.Labels,
                ConsumptionValues = stats.Values
            };

            return View(model);
        }

        public IActionResult Error() => View();
    }
}

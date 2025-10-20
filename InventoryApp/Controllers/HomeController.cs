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
        public HomeController(IComponentService components) { _components = components; }


        public async Task<IActionResult> Index()
        {
            var all = await _components.GetAllAsync();
            var low = await _components.GetLowStockAsync(5);


            var model = new HomeViewModel
            {
                TotalComponents = all.Count,
                TotalQuantity = all.Sum(c => c.Quantity),
                LowStockCount = low.Count,
                LowStock = low
            };
            return View(model);
        }


        public IActionResult Error() => View();
    }
}

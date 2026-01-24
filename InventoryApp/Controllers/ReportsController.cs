using InventoryApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Controllers
{
    public class ReportsController : Controller
    {
        private readonly AppDbContext _db;

        public ReportsController(AppDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(LowStock));
        }

        public async Task<IActionResult> LowStock(string filter = "all")
        {
            filter = (filter ?? "all").ToLowerInvariant();

            var components = await _db.Components
                .Include(c => c.Location)
                .OrderBy(c => c.Quantity)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var rows = components
                .Where(c => c.Quantity < (c.ReorderPoint ?? 5))
                .Select(c =>
                {
                    int reorderPoint = c.ReorderPoint ?? 5;
                    int qty = c.Quantity;

                    return new LowStockRow
                    {
                        ComponentId = c.Id,
                        Name = c.Name,
                        ManufacturerPartNumber = c.ManufacturerPartNumber,
                        Quantity = qty,
                        ReorderPoint = reorderPoint,
                        ToBuy = Math.Max(reorderPoint - qty, 0),
                        LocationDisplay = c.Location != null ? c.Location.DisplayName : "–"
                    };
                })
                .ToList();

            rows = filter switch
            {
                "out" => rows.Where(r => r.Quantity == 0).ToList(),                    
                "low" => rows.Where(r => r.Quantity > 0).ToList(),                     
                _ => rows
            };

            ViewBag.Filter = filter;
            return View(rows);
        }

        public class LowStockRow
        {
            public int ComponentId { get; set; }
            public string Name { get; set; } = "";
            public string? ManufacturerPartNumber { get; set; }
            public int Quantity { get; set; }
            public int ReorderPoint { get; set; }
            public int ToBuy { get; set; }
            public string LocationDisplay { get; set; } = "–";
        }

        public async Task<IActionResult> Consumption(int days = 30, bool projectsOnly = false)
        {
            if (days <= 0) days = 30;
            if (days > 365) days = 365;

            var since = DateTime.UtcNow.AddDays(-days);

            var q = _db.InventoryTransactions.AsNoTracking()
                .Where(t => t.CreatedAt >= since && t.DeltaQuantity < 0);

            if (projectsOnly)
                q = q.Where(t => t.ProjectId != null);

            var rows = await q
                .GroupBy(t => t.ComponentId)
                .Select(g => new ConsumptionRow
                {
                    ComponentId = g.Key,
                    UsedFromProjects = -g.Where(x => x.ProjectId != null).Sum(x => x.DeltaQuantity),
                    UsedManual = -g.Where(x => x.ProjectId == null).Sum(x => x.DeltaQuantity),
                    Used = -g.Sum(x => x.DeltaQuantity),
                    UsesCount = g.Count()
                })
                .OrderByDescending(r => r.Used)
                .Take(50)
                .ToListAsync();

            var ids = rows.Select(r => r.ComponentId).ToList();

            var components = await _db.Components
                .AsNoTracking()
                .Where(c => ids.Contains(c.Id))
                .Select(c => new { c.Id, c.Name, c.ManufacturerPartNumber, c.Quantity, ReorderPoint = (c.ReorderPoint ?? 5) })
                .ToListAsync();

            var map = components.ToDictionary(x => x.Id, x => x);

            foreach (var r in rows)
            {
                if (map.TryGetValue(r.ComponentId, out var c))
                {
                    r.Name = c.Name;
                    r.ManufacturerPartNumber = c.ManufacturerPartNumber;
                    r.Quantity = c.Quantity;
                    r.ReorderPoint = c.ReorderPoint;
                    r.ToBuy = Math.Max(c.ReorderPoint - c.Quantity, 0);
                }
            }

            ViewBag.Days = days;
            ViewBag.ProjectsOnly = projectsOnly;
            return View(rows);
        }

        public class ConsumptionRow
        {
            public int ComponentId { get; set; }
            public string Name { get; set; } = "";
            public string? ManufacturerPartNumber { get; set; }
            public int Used { get; set; }             
            public int UsesCount { get; set; }        
            public int Quantity { get; set; }         
            public int ReorderPoint { get; set; }     
            public int ToBuy { get; set; }            
            public int UsedFromProjects { get; set; }
            public int UsedManual { get; set; }

            public string SourceLabel =>
                UsedFromProjects > 0 && UsedManual > 0 ? "Mix" :
                UsedFromProjects > 0 ? "Projekty" :
                "Ruční";
        }
    }
}

using InventoryApp.Models;
using InventoryApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryApp.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IProjectPlanningService _planningService;

        private readonly SupplierAggregatorService _supplierAggregator;

        public ProjectsController(AppDbContext context, IProjectPlanningService planningService, SupplierAggregatorService supplierAggregator)
        {
            _context = context;
            _planningService = planningService;
            _supplierAggregator = supplierAggregator;
        }

        // GET: Projects
        public async Task<IActionResult> Index()
        {
            return View(await _context.Projects.OrderByDescending(p => p.CreatedAt).ToListAsync());
        }

        // GET: Projects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .Include(p => p.Items)
                    .ThenInclude(i => i.Component)
                .Include(p => p.Items)
                    .ThenInclude(i => i.SupplierOffers)
                        .ThenInclude(o => o.Supplier)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                return NotFound();
            }

            if (project.ConsumedAt == null)
            {
                _planningService.RecalculateInMemory(project);
            }

            var components = await _context.Components
                .OrderBy(c => c.Name)
                .ToListAsync();

            var componentItems = components
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name           
                })
                .ToList();

            // placeholder na první pozici
            componentItems.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "-- vyber součástku --"
            });

            ViewData["ComponentId"] = componentItems;

            ViewBag.Error = TempData["Error"];

            return View(project);
        }

        // GET: Projects/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Projects/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description")] Project project)
        {
            if (ModelState.IsValid)
            {
                project.CreatedAt = DateTime.UtcNow;
                _context.Add(project);
                await _context.SaveChangesAsync();

                TempData["ToastSuccess"] = $"Projekt „{project.Name}“ byl úspěšně vytvořen.";

                return RedirectToAction(nameof(Details), new { id = project.Id });
            }

            return View(project);
        }

        // POST: /Projects/AddItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItem(int projectId, int? componentId, string? customName, int quantityRequired)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
            {
                return NotFound();
            }

            if (componentId == null && string.IsNullOrWhiteSpace(customName))
            {
                TempData["Error"] = "Vyber součástku nebo vyplň vlastní název.";
                return RedirectToAction(nameof(Details), new { id = projectId });
            }

            if (quantityRequired <= 0)
            {
                TempData["Error"] = "Počet kusů musí být větší než 0.";
                return RedirectToAction(nameof(Details), new { id = projectId });
            }

            var item = new ProjectItem
            {
                ProjectId = projectId,
                ComponentId = componentId,
                CustomName = componentId.HasValue ? null : customName,
                QuantityRequired = quantityRequired,
                QuantityFromStock = 0,
                QuantityToBuy = quantityRequired
            };

            _context.ProjectItems.Add(item);
            await _context.SaveChangesAsync();

            await _planningService.RecalculateAndSaveAsync(projectId);

            return RedirectToAction(nameof(Details), new { id = projectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteItem(int id, int projectId)
        {
            var item = await _context.ProjectItems.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            _context.ProjectItems.Remove(item);
            await _context.SaveChangesAsync();

            await _planningService.RecalculateAndSaveAsync(projectId);

            return RedirectToAction(nameof(Details), new { id = projectId });
        }

        // GET: Projects/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }
            return View(project);
        }

        // POST: Projects/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,CreatedAt")] Project project)
        {
            if (id != project.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(project);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProjectExists(project.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(project);
        }

        // GET: Projects/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .FirstOrDefaultAsync(m => m.Id == id);
            if (project == null)
            {
                return NotFound();
            }

            if (project.ConsumedAt != null)
            {
                TempData["ToastError"] = "Uzamčený projekt nelze smazat.";
                return RedirectToAction(nameof(Index));
            }

            return View(project);
        }

        // POST: Projects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
                return NotFound();

            if (project.ConsumedAt != null)
            {
                TempData["ToastError"] = "Uzamčený projekt nelze smazat.";
                return RedirectToAction(nameof(Index));
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private bool ProjectExists(int id)
        {
            return _context.Projects.Any(e => e.Id == id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FindOffers(int projectId)
        {
            var exists = await _context.Projects.AnyAsync(p => p.Id == projectId);
            if (!exists)
            {
                return NotFound();
            }

            await _planningService.RecalculateAndSaveAsync(projectId);

            await _supplierAggregator.GenerateOffersForProjectAsync(projectId);

            TempData["OffersSearched"] = true;

            return RedirectToAction(nameof(Details), new { id = projectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectOffer(int offerId, int projectId)
        {
            var offer = await _context.SupplierOffers
                .Include(o => o.ProjectItem)
                .FirstOrDefaultAsync(o => o.Id == offerId);

            if (offer == null || offer.ProjectItem == null)
            {
                return NotFound();
            }

            var projectItemId = offer.ProjectItemId;

            var allOffersForItem = await _context.SupplierOffers
                .Where(o => o.ProjectItemId == projectItemId)
                .ToListAsync();

            foreach (var o in allOffersForItem)
            {
                o.IsSelected = false;
            }

            offer.IsSelected = true;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = projectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AutoSelectCheapest(int projectId)
        {
            var project = await _context.Projects
                .Include(p => p.Items)
                    .ThenInclude(i => i.SupplierOffers)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
            {
                return NotFound();
            }

            foreach (var item in project.Items)
            {
                if (item.QuantityToBuy <= 0 ||
                    item.SupplierOffers == null ||
                    !item.SupplierOffers.Any())
                {
                    continue;
                }

                var cheapest = item.SupplierOffers
                    .OrderBy(o => o.UnitPrice)
                    .FirstOrDefault();

                if (cheapest == null)
                {
                    continue;
                }

                foreach (var offer in item.SupplierOffers)
                {
                    offer.IsSelected = offer.Id == cheapest.Id;
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = projectId });
        }

        [HttpGet]
        public async Task<IActionResult> ExportOrderCsv(int projectId)
        {
            var project = await _context.Projects
                .Include(p => p.Items)
                    .ThenInclude(i => i.Component)
                .Include(p => p.Items)
                    .ThenInclude(i => i.SupplierOffers)
                        .ThenInclude(o => o.Supplier)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
            {
                return NotFound();
            }

            var selectedOffers = project.Items
                .Where(i => i.SupplierOffers != null && i.SupplierOffers.Any(o => o.IsSelected))
                .SelectMany(i => i.SupplierOffers
                    .Where(o => o.IsSelected)
                    .Select(o => new
                    {
                        Item = i,
                        Offer = o,
                        SupplierName = o.Supplier != null ? o.Supplier.Name : "Unknown"
                    }))
                .ToList();

            if (!selectedOffers.Any())
            {
                return BadRequest("Žádné vybrané nabídky k exportu.");
            }

            var sb = new StringBuilder();

            sb.AppendLine("Dodavatel;Součástka;Počet;Cena/ks;Měna;Celkem");

            foreach (var x in selectedOffers)
            {
                var item = x.Item;
                var offer = x.Offer;

                string partName;
                if (item.Component != null)
                    partName = item.Component.Name;
                else if (!string.IsNullOrWhiteSpace(item.CustomName))
                    partName = item.CustomName;
                else
                    partName = "Unknown";

                int qtyToBuy = item.QuantityToBuy;
                decimal total = qtyToBuy * offer.UnitPrice;

                string Escape(string value)
                {
                    if (string.IsNullOrEmpty(value))
                        return "";

                    if (value.Contains(';') || value.Contains('"'))
                    {
                        return "\"" + value.Replace("\"", "\"\"") + "\"";
                    }

                    return value;
                }

                sb.AppendLine(string.Join(";", new[]
                {
            Escape(x.SupplierName),
            Escape(partName),
            qtyToBuy.ToString(),
            offer.UnitPrice.ToString("0.00"),
            offer.Currency,
            total.ToString("0.00")
        }));
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"Order_Project_{project.Id}.csv";

            return File(bytes, "text/csv", fileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConsumeFromStock(int id)
        {
            var project = await _context.Projects
                .Include(p => p.Items)
                    .ThenInclude(i => i.Component)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();

            if (project.ConsumedAt != null)
            {
                TempData["ToastError"] = "Tento projekt už byl ze skladu odečten.";
                return RedirectToAction(nameof(Details), new { id });
            }

            foreach (var item in project.Items.Where(i => i.ComponentId != null && i.QuantityFromStock > 0))
            {
                if (item.Component == null) continue;

                if (item.QuantityFromStock > item.Component.Quantity)
                {
                    TempData["ToastError"] =
                        $"Nelze odečíst {item.QuantityFromStock} ks z '{item.Component.Name}'. " +
                        $"Na skladě je jen {item.Component.Quantity} ks.";
                    return RedirectToAction(nameof(Details), new { id });
                }
            }

            using var tx = await _context.Database.BeginTransactionAsync();

            foreach (var item in project.Items.Where(i => i.ComponentId != null && i.QuantityFromStock > 0))
            {
                if (item.Component == null) continue;

                item.Component.Quantity -= item.QuantityFromStock;

                _context.InventoryTransactions.Add(new InventoryTransaction
                {
                    ComponentId = item.Component.Id,
                    DeltaQuantity = -item.QuantityFromStock,
                    Type = InventoryTransactionType.Use,
                    ProjectId = project.Id,
                    Note = $"Projekt: {project.Name}"
                });
            }

            project.ConsumedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}

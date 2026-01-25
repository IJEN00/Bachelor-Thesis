using InventoryApp.Models;
using InventoryApp.Services;
using InventoryApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InventoryApp.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly IProjectService _svc;
        private readonly IComponentService _componentSvc; 

        public ProjectsController(IProjectService svc, IComponentService componentSvc)
        {
            _svc = svc;
            _componentSvc = componentSvc;
        }

        // GET: Projects
        public async Task<IActionResult> Index()
        {
            var list = await _svc.GetAllAsync();
            return View(list);
        }

        // GET: Projects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var project = await _svc.GetDetailsAsync(id.Value);
            if (project == null) return NotFound();

            var components = await _componentSvc.GetAllAsync();
            var componentItems = components
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.Name} (Skladem: {c.Quantity} ks)" 
                })
                .ToList();

            componentItems.Insert(0, new SelectListItem { Value = "", Text = "-- vyber součástku --" });

            var vm = new ProjectDetailViewModel
            {
                Project = project,
                AvailableComponents = componentItems,
                QuantityRequired = 1,
                OffersSearched = TempData["OffersSearched"] as bool? ?? false
            };

            ViewBag.Error = TempData["Error"];
            return View(vm);
        }

        // GET: Projects/Create
        public IActionResult Create() => View();

        // POST: Projects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description")] Project project)
        {
            if (ModelState.IsValid)
            {
                await _svc.CreateAsync(project);
                TempData["ToastSuccess"] = $"Projekt „{project.Name}“ byl vytvořen.";
                return RedirectToAction(nameof(Details), new { id = project.Id });
            }
            return View(project);
        }

        // GET: Projects/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var project = await _svc.GetByIdAsync(id.Value);
            if (project == null) return NotFound();
            return View(project);
        }

        // POST: Projects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,CreatedAt")] Project project)
        {
            if (id != project.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    await _svc.UpdateAsync(project);
                    return RedirectToAction(nameof(Index));
                }
                catch (KeyNotFoundException)
                {
                    return NotFound();
                }
            }
            return View(project);
        }

        // GET: Projects/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var project = await _svc.GetByIdAsync(id.Value);
            if (project == null) return NotFound();

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
            try
            {
                await _svc.DeleteAsync(id);
                TempData["ToastSuccess"] = "Projekt smazán.";
            }
            catch (Exception ex)
            {
                TempData["ToastError"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItem(int projectId, int? selectedComponentId, string? customName, int quantityRequired)
        {
            try
            {
                await _svc.AddItemAsync(projectId, selectedComponentId, customName, quantityRequired);
            }
            catch (ArgumentException ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Details), new { id = projectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteItem(int id, int projectId) 
        {
            try
            {
                await _svc.DeleteItemAsync(id);
            }
            catch (Exception)
            {
            }
            return RedirectToAction(nameof(Details), new { id = projectId });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FindOffers(int projectId)
        {
            await _svc.FindOffersAsync(projectId);
            TempData["OffersSearched"] = true;
            return RedirectToAction(nameof(Details), new { id = projectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectOffer(int offerId, int projectId)
        {
            await _svc.SelectOfferAsync(offerId);
            return RedirectToAction(nameof(Details), new { id = projectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AutoSelectCheapest(int projectId)
        {
            await _svc.AutoSelectCheapestAsync(projectId);
            TempData["ToastSuccess"] = "Vybrány nejlevnější nabídky.";
            return RedirectToAction(nameof(Details), new { id = projectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConsumeFromStock(int id)
        {
            try
            {
                await _svc.ConsumeStockAsync(id);
                TempData["ToastSuccess"] = "Součástky byly úspěšně vyskladněny.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ToastError"] = ex.Message; 
            }
            catch (Exception)
            {
                TempData["ToastError"] = "Chyba při vyskladnění.";
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> ExportOrderCsv(int projectId)
        {
            try
            {
                var bytes = await _svc.GenerateOrderCsvAsync(projectId);
                return File(bytes, "text/csv", $"Order_Project_{projectId}.csv");
            }
            catch (Exception ex)
            {
                TempData["ToastError"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id = projectId });
            }
        }
    }
}
using InventoryApp.Models;
using InventoryApp.Services;
using InventoryApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace InventoryApp.Controllers
{
    public class ComponentsController : Controller
    {
        private readonly IComponentService _svc;
        private readonly ILocationService _locationSvc;
        private readonly IDocumentService _docSvc;
        private readonly IWebHostEnvironment _env;
        private readonly SupplierAggregatorService _aggregator;

        public ComponentsController(IComponentService svc, ILocationService locationSvc, IDocumentService doc, IWebHostEnvironment env, SupplierAggregatorService aggregator)
        {
            _svc = svc;
            _locationSvc = locationSvc;
            _env = env;
            _docSvc = doc;
            _aggregator = aggregator;
        }


        public async Task<IActionResult> Index()
        {
            var list = await _svc.GetAllAsync();
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Filter(string? rack, string? drawer, string? box, string? search)
        {
            var list = await _svc.FilterComponentsAsync(rack, drawer, box, search);

            return PartialView("_ComponentListPartial", list);
        }


        public async Task<IActionResult> Create()
        {
            var locations = await _locationSvc.GetAllAsync();

            var vm = new ComponentViewModel
            {
                LocationOptions = new SelectList(locations, "Id", "DisplayName"),
                ReorderPoint = 5 
            };

            return View(vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ComponentViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                var locations = await _locationSvc.GetAllAsync();
                vm.LocationOptions = new SelectList(locations, "Id", "DisplayName", vm.LocationId);
                return View(vm);
            }

            var component = new Models.Component
            {
                Name = vm.Name,
                Manufacturer = vm.Manufacturer,
                ManufacturerPartNumber = vm.ManufacturerPartNumber,
                Package = vm.Package, 
                Quantity = vm.Quantity,
                ReorderPoint = vm.ReorderPoint,
                LocationId = vm.LocationId
            };

            await _svc.AddAsync(component);

            await _docSvc.UploadFilesAsync(vm.Files, component.Id, _env.WebRootPath);

            TempData["ToastSuccess"] = $"Součástka „{component.Name}“ byla vytvořena.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var component = await _svc.GetByIdAsync(id);
            if (component == null) return NotFound();

            var locations = await _locationSvc.GetAllAsync();

            var vm = new ComponentViewModel
            {
                Id = component.Id,
                Name = component.Name,
                Manufacturer = component.Manufacturer,
                ManufacturerPartNumber = component.ManufacturerPartNumber,
                Package = component.Package, 
                Quantity = component.Quantity,
                ReorderPoint = component.ReorderPoint,
                LocationId = component.LocationId,
                ExistingDocuments = component.Documents ?? new List<Document>(),
                LocationOptions = new SelectList(locations, "Id", "DisplayName", component.LocationId)
            };

            return View(vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ComponentViewModel vm)
        {
            if (id != vm.Id) return BadRequest();

            if (!ModelState.IsValid)
            {
                var locations = await _locationSvc.GetAllAsync();
                vm.LocationOptions = new SelectList(locations, "Id", "DisplayName", vm.LocationId);

                var original = await _svc.GetByIdAsync(id);
                vm.ExistingDocuments = original?.Documents ?? new List<Document>();
                return View(vm);
            }

            var componentToUpdate = await _svc.GetByIdAsync(id);
            if (componentToUpdate == null) return NotFound();

            componentToUpdate.Name = vm.Name;
            componentToUpdate.Manufacturer = vm.Manufacturer;
            componentToUpdate.ManufacturerPartNumber = vm.ManufacturerPartNumber;
            componentToUpdate.Package = vm.Package; 
            componentToUpdate.ReorderPoint = vm.ReorderPoint;
            componentToUpdate.LocationId = vm.LocationId;

            await _svc.UpdateAsync(componentToUpdate);

            await _docSvc.UploadFilesAsync(vm.Files, componentToUpdate.Id, _env.WebRootPath);

            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> Details(int id)
        {
            var component = await _svc.GetByIdAsync(id);
            if (component == null) return NotFound();

            var history = await _svc.GetTransactionHistoryAsync(id);

            var vm = new ComponentDetailViewModel
            {
                Id = component.Id,
                Name = component.Name,
                Manufacturer = component.Manufacturer,
                ManufacturerPartNumber = component.ManufacturerPartNumber,
                Package = component.Package,
                Quantity = component.Quantity,
                ReorderPoint = component.ReorderPoint ?? 5,
                Location = component.Location,
                Documents = component.Documents.ToList(),
                History = history 
            };

            return View(vm);
        }


        public async Task<IActionResult> Delete(int id)
        {
            var model = await _svc.GetByIdAsync(id);
            if (model == null) return NotFound();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            await _docSvc.DeleteAsync(id);
            return Ok();
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _svc.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStock(int id, int amount, string? note)
        {
            try
            {
                await _svc.AddStockAsync(id, amount, note);
                TempData["ToastSuccess"] = "Součástka byla úspěšně naskladněna.";
            }
            catch (ArgumentException ex)
            {
                TempData["ToastError"] = ex.Message;
            }
            catch (Exception ex)
            {
                TempData["ToastError"] = $"Chyba při naskladnění: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UseStock(int id, int amount, string? note)
        {
            try
            {
                await _svc.UseStockAsync(id, amount, note);
                TempData["ToastSuccess"] = "Součástka byla úspěšně vyskladněna.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ToastError"] = ex.Message;
            }
            catch (ArgumentException ex)
            {
                TempData["ToastError"] = ex.Message;
            }
            catch (Exception ex)
            {
                TempData["ToastError"] = $"Chyba při vyskladnění: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> SearchOffers(int id)
        {
            var component = await _svc.GetByIdAsync(id);
            if (component == null) return NotFound();

            var offers = await _aggregator.SearchForComponentAsync(component);

            return PartialView("_SupplierOffersPartial", offers);
        }
    }
}

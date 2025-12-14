using InventoryApp.Models;
using InventoryApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Controllers
{
    public class ComponentsController : Controller
    {
        private readonly IComponentService _svc;
        private readonly ILocationService _locationSvc;
        private readonly IDocumentService _docSvc;
        private readonly IWebHostEnvironment _env;
        private readonly AppDbContext _context;

        public ComponentsController(IComponentService svc, ILocationService locationSvc, IDocumentService doc, IWebHostEnvironment env, AppDbContext context)
        {
            _svc = svc;
            _locationSvc = locationSvc;
            _env = env;
            _docSvc = doc;
            _context = context;
        }


        public async Task<IActionResult> Index()
        {
            var list = await _svc.GetAllAsync();
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Filter(string? rack, string? drawer, string? box, string? search)
        {
            var list = await _svc.GetAllAsync();

            if (!string.IsNullOrEmpty(rack))
                list = (list.Where(c => c.Location != null && c.Location.Rack == rack)).ToList();

            if (!string.IsNullOrEmpty(drawer))
                list = (list.Where(c => c.Location != null && c.Location.Drawer == drawer)).ToList();

            if (!string.IsNullOrEmpty(box))
                list = (list.Where(c => c.Location != null && c.Location.Box == box)).ToList();

            if (!string.IsNullOrEmpty(search))
                list = (list.Where(c => c.Name.Contains(search, StringComparison.OrdinalIgnoreCase))).ToList();

            return PartialView("_ComponentListPartial", list);
        }


        public async Task<IActionResult> Create()
        {
            var locations = await _locationSvc.GetAllAsync();
            ViewBag.LocationId = new SelectList(locations, "Id", "DisplayName");
            return View(new Component());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Component c, List<IFormFile> files)
        {
            if (!ModelState.IsValid)
            {
                var locations = await _locationSvc.GetAllAsync();
                ViewBag.LocationId = new SelectList(locations, "Id", "DisplayName");
                return View(c);
            }

            await _svc.AddAsync(c);

            if (files != null && files.Count > 0)
            {
                var uploadsPath = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsPath);

                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file.FileName);
                    var filePath = Path.Combine("uploads", $"{Guid.NewGuid()}_{fileName}");
                    using (var stream = new FileStream(Path.Combine(_env.WebRootPath, filePath), FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var doc = new Document
                    {
                        ComponentId = c.Id,
                        FilePath = filePath,
                        FileName = fileName,
                        UploadedAt = DateTime.UtcNow
                    };
                    await _docSvc.AddAsync(doc);
                }
            }

            TempData["ToastSuccess"] = $"Součástka „{c.Name}“ byla vytvořena.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var model = await _svc.GetByIdAsync(id);
            if (model == null) return NotFound();

            var locations = await _locationSvc.GetAllAsync();
            ViewBag.LocationId = new SelectList(locations, "Id", "DisplayName", model.LocationId);
            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Component c, List<IFormFile> files)
        {
            if (id != c.Id) return BadRequest();

            if (!ModelState.IsValid)
            {
                var locations = await _locationSvc.GetAllAsync();
                ViewBag.LocationId = new SelectList(locations, "Id", "DisplayName", c.LocationId);
                return View(c);
            }

            await _svc.UpdateAsync(c);

            if (files != null && files.Count > 0)
            {
                var uploadsPath = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsPath);

                foreach (var file in files)
                {
                    if (file.Length <= 0) continue;

                    var fileName = Path.GetFileName(file.FileName);
                    var filePath = Path.Combine("uploads", $"{Guid.NewGuid()}_{fileName}");

                    using (var stream = new FileStream(Path.Combine(_env.WebRootPath, filePath), FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var doc = new Document
                    {
                        ComponentId = c.Id,
                        FilePath = filePath,
                        FileName = fileName,
                        UploadedAt = DateTime.UtcNow
                    };
                    await _docSvc.AddAsync(doc);
                }
            }

            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> Details(int id)
        {
            var model = await _svc.GetByIdAsync(id);
            if (model == null) return NotFound();
            model.Documents = (await _docSvc.GetByIdAsync(id)).ToList();
            model.Location = await _locationSvc.GetByIdAsync(model.LocationId ?? 0);

            var tx = await _context.InventoryTransactions
                .Where(t => t.ComponentId == id)
                .OrderByDescending(t => t.CreatedAt)
                .Take(10)
                .ToListAsync();

            ViewBag.RecentTransactions = tx;

            return View(model);
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
            if (amount <= 0)
            {
                TempData["ToastError"] = "Množství musí být větší než 0.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var component = await _context.Components.FirstOrDefaultAsync(c => c.Id == id);
            if (component == null) return NotFound();

            component.Quantity = (byte)Math.Min(component.Quantity + amount, byte.MaxValue);

            _context.InventoryTransactions.Add(new InventoryTransaction
            {
                ComponentId = id,
                DeltaQuantity = amount,
                Type = InventoryTransactionType.Add,
                Note = string.IsNullOrWhiteSpace(note) ? null : note
            });

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UseStock(int id, int amount, string? note)
        {
            if (amount <= 0)
            {
                TempData["ToastError"] = "Množství musí být větší než 0.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var component = await _context.Components.FirstOrDefaultAsync(c => c.Id == id);
            if (component == null) return NotFound();

            if (amount > component.Quantity)
            {
                TempData["ToastError"] = $"Nelze odebrat {amount} ks. Na skladě je jen {component.Quantity} ks.";
                return RedirectToAction(nameof(Details), new { id });
            }

            component.Quantity = (byte)(component.Quantity - amount);

            _context.InventoryTransactions.Add(new InventoryTransaction
            {
                ComponentId = id,
                DeltaQuantity = -amount,
                Type = InventoryTransactionType.Use,
                Note = string.IsNullOrWhiteSpace(note) ? null : note
            });

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}

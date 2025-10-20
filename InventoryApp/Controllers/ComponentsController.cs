using InventoryApp.Models;
using InventoryApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InventoryApp.Controllers
{
    public class ComponentsController : Controller
    {
        private readonly IComponentService _svc;
        private readonly ILocationService _locationSvc;
        private readonly IDocumentService _docSvc;
        private readonly IWebHostEnvironment _env;

        public ComponentsController(IComponentService svc, ILocationService locationSvc, IDocumentService doc, IWebHostEnvironment env)
        {
            _svc = svc;
            _locationSvc = locationSvc;
            _env = env;
            _docSvc = doc;
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
    }
}

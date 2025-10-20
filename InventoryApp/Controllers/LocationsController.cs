using InventoryApp.Models;
using InventoryApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryApp.Controllers
{
    public class LocationsController : Controller
    {
        private readonly ILocationService _svc;

        public LocationsController(ILocationService svc)
        {
            _svc = svc;
        }

        // GET: Locations
        public async Task<IActionResult> Index()
        {
            var list = await _svc.GetAllAsync();
            return View(list);
        }

        public async Task<IActionResult> Filter(string rack, string drawer)
        {
            var query = await _svc.GetAllAsync();

            if (!string.IsNullOrEmpty(rack))
                query = query.Where(l => l.Rack == rack);

            if (!string.IsNullOrEmpty(drawer))
                query = query.Where(l => l.Drawer == drawer);

            return PartialView("_LocationListPartial", query.ToList());
        }

        // GET: Locations/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Locations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Rack,Drawer,Box")] Location location)
        {
            if (!ModelState.IsValid) return View(location);

            await _svc.AddAsync(location); 
            return RedirectToAction(nameof(Index));
        }

        // GET: Locations/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var model = await _svc.GetByIdAsync(id);
            if (model == null) return NotFound();
            return View(model);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _svc.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<JsonResult> GetRacks()
        {
            var racks = (await _svc.GetAllAsync())
                .Where(l => !string.IsNullOrEmpty(l.Rack))
                .Select(l => new { rack = l.Rack }) 
                .Distinct()
                .OrderBy(l => l.rack)
                .ToList();
            return Json(racks);
        }

        [HttpGet]
        public async Task<JsonResult> GetDrawers(string rack)
        {
            var drawers = (await _svc.GetAllAsync())
                .Where(l => l.Rack == rack && !string.IsNullOrEmpty(l.Drawer))
                .Select(l => new { drawer = l.Drawer }) 
                .Distinct()
                .OrderBy(l => l.drawer)
                .ToList();
            return Json(drawers);
        }

        [HttpGet]
        public async Task<JsonResult> GetBoxes(string rack, string drawer)
        {
            var boxes = (await _svc.GetAllAsync())
                .Where(l => l.Rack == rack && l.Drawer == drawer && !string.IsNullOrEmpty(l.Box))
                .Select(l => new { id = l.Id, box = l.Box }) 
                .OrderBy(l => l.box)
                .ToList();
            return Json(boxes);
        }

        [HttpGet]
        public async Task<JsonResult> GetLocationById(int id)
        {
            var loc = await _svc.GetByIdAsync(id);
            if (loc == null)
                return Json(null);

            return Json(new
            {
                id = loc.Id,
                rack = loc.Rack,
                drawer = loc.Drawer,
                box = loc.Box
            });
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System;

namespace InventoryApp.Controllers
{
    public class SettingsController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public SettingsController(IWebHostEnvironment env)
        {
            _env = env;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult DownloadBackup()
        {
            string dbFileName = "inventory.db";
            string filepath = Path.Combine(_env.ContentRootPath, dbFileName);

            if (!System.IO.File.Exists(filepath))
            {
                TempData["Error"] = "Soubor databáze nebyl na serveru nalezen.";
                return RedirectToAction(nameof(Index));
            }

            string downloadName = $"ElectroVentory_Backup_{DateTime.Now:yyyyMMdd_HHmm}.db";

            return PhysicalFile(filepath, "application/octet-stream", downloadName);
        }
    }
}

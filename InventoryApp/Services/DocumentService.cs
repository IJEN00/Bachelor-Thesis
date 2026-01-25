using InventoryApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public DocumentService(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public async Task<IEnumerable<Document>> GetByIdAsync(int componentId)
        {
            return await _db.Documents
                .Where(d => d.ComponentId == componentId)
                .ToListAsync();
        }

        public async Task AddAsync(Document doc)
        {
            _db.Documents.Add(doc);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var doc = await _db.Documents.FindAsync(id);
            if (doc != null)
            {
                var path = Path.Combine(_env.WebRootPath, doc.FilePath);
                if (File.Exists(path)) File.Delete(path);

                _db.Documents.Remove(doc);
                await _db.SaveChangesAsync();
            }
        }

        public async Task UploadFilesAsync(List<IFormFile> files, int componentId, string webRootPath)
        {
            if (files == null || files.Count == 0) return;

            var uploadsPath = Path.Combine(webRootPath, "uploads");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            foreach (var file in files)
            {
                if (file.Length <= 0) continue;

                var fileName = Path.GetFileName(file.FileName);
                var uniqueName = $"{Guid.NewGuid()}_{fileName}";
                var relativePath = Path.Combine("uploads", uniqueName);
                var fullPath = Path.Combine(webRootPath, relativePath);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var doc = new Document
                {
                    ComponentId = componentId,
                    FilePath = relativePath, 
                    FileName = fileName,
                    UploadedAt = DateTime.UtcNow
                };

                _db.Documents.Add(doc);
            }

            await _db.SaveChangesAsync();
        }
    }
}

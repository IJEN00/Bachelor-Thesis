using System.Text;
using InventoryApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Services
{
    public class ProjectService : IProjectService
    {
        private readonly AppDbContext _context;
        private readonly IProjectPlanningService _planningService;
        private readonly SupplierAggregatorService _aggregator;

        public ProjectService(AppDbContext context, IProjectPlanningService planningService, SupplierAggregatorService aggregator)
        {
            _context = context;
            _planningService = planningService;
            _aggregator = aggregator;
        }

        public async Task<List<Project>> GetAllAsync()
        {
            return await _context.Projects
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Project?> GetByIdAsync(int id)
        {
            return await _context.Projects.FindAsync(id);
        }

        public async Task<Project?> GetDetailsAsync(int id)
        {
            var project = await _context.Projects
                .Include(p => p.Items)
                    .ThenInclude(i => i.Component)
                .Include(p => p.Items)
                    .ThenInclude(i => i.SupplierOffers)
                        .ThenInclude(o => o.Supplier)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project != null && project.ConsumedAt == null)
            {
                _planningService.RecalculateInMemory(project);
            }

            return project;
        }

        public async Task CreateAsync(Project project)
        {
            project.CreatedAt = DateTime.UtcNow;
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Project project)
        {
            try
            {
                _context.Projects.Update(project);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProjectExists(project.Id)) throw new KeyNotFoundException("Projekt neexistuje.");
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null) throw new KeyNotFoundException("Projekt nenalezen.");

            if (project.ConsumedAt != null)
                throw new InvalidOperationException("Uzamčený (vyskladněný) projekt nelze smazat.");

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
        }

        public bool ProjectExists(int id)
        {
            return _context.Projects.Any(e => e.Id == id);
        }

        public async Task AddItemAsync(int projectId, int? componentId, string? customName, int quantity)
        {
            if (quantity <= 0) throw new ArgumentException("Počet kusů musí být > 0");

            var item = new ProjectItem
            {
                ProjectId = projectId,
                ComponentId = componentId,
                CustomName = componentId.HasValue ? null : customName,
                QuantityRequired = quantity,
                QuantityToBuy = quantity 
            };

            _context.ProjectItems.Add(item);
            await _context.SaveChangesAsync();

            await _planningService.RecalculateAndSaveAsync(projectId);
        }

        public async Task DeleteItemAsync(int itemId)
        {
            var item = await _context.ProjectItems.FindAsync(itemId);
            if (item == null) throw new KeyNotFoundException("Položka nenalezena.");

            int projectId = item.ProjectId;
            _context.ProjectItems.Remove(item);
            await _context.SaveChangesAsync();

            await _planningService.RecalculateAndSaveAsync(projectId);
        }

        public async Task FindOffersAsync(int projectId)
        {
            var exists = await _context.Projects.AnyAsync(p => p.Id == projectId);
            if (!exists) throw new KeyNotFoundException("Projekt nenalezen.");

            await _planningService.RecalculateAndSaveAsync(projectId);

            await _aggregator.GenerateOffersForProjectAsync(projectId);
        }

        public async Task SelectOfferAsync(int offerId)
        {
            var offer = await _context.SupplierOffers
                .FirstOrDefaultAsync(o => o.Id == offerId);

            if (offer == null) throw new KeyNotFoundException("Nabídka nenalezena.");

            var otherOffers = await _context.SupplierOffers
                .Where(o => o.ProjectItemId == offer.ProjectItemId)
                .ToListAsync();

            foreach (var o in otherOffers) o.IsSelected = false;

            offer.IsSelected = true;
            await _context.SaveChangesAsync();
        }

        public async Task ConsumeStockAsync(int projectId)
        {
            var project = await _context.Projects
                .Include(p => p.Items)
                .ThenInclude(i => i.Component)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null) throw new KeyNotFoundException("Projekt nenalezen.");
            if (project.ConsumedAt != null) throw new InvalidOperationException("Projekt již byl vyskladněn.");

            foreach (var item in project.Items.Where(i => i.ComponentId != null && i.QuantityFromStock > 0))
            {
                if (item.Component == null) continue;
                if (item.QuantityFromStock > item.Component.Quantity)
                {
                    throw new InvalidOperationException(
                        $"Nelze odečíst {item.QuantityFromStock} ks z '{item.Component.Name}'. Skladem jen {item.Component.Quantity}.");
                }
            }

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var item in project.Items.Where(i => i.ComponentId != null && i.QuantityFromStock > 0))
                {
                    if (item.Component == null) continue;

                    item.Component.Quantity -= (byte)item.QuantityFromStock; 

                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        ComponentId = item.Component.Id,
                        DeltaQuantity = -item.QuantityFromStock,
                        Type = InventoryTransactionType.Use,
                        ProjectId = project.Id,
                        Note = $"Projekt: {project.Name}",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                project.ConsumedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task AutoSelectCheapestAsync(int projectId)
        {
            var project = await _context.Projects
                .Include(p => p.Items)
                .ThenInclude(i => i.SupplierOffers)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null) return;

            foreach (var item in project.Items)
            {
                if (item.QuantityToBuy <= 0 || item.SupplierOffers == null || !item.SupplierOffers.Any())
                    continue;

                var cheapest = item.SupplierOffers.OrderBy(o => o.UnitPrice).FirstOrDefault();
                if (cheapest == null) continue;

                foreach (var offer in item.SupplierOffers)
                {
                    offer.IsSelected = (offer.Id == cheapest.Id);
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task<byte[]> GenerateOrderCsvAsync(int projectId)
        {
            var project = await GetDetailsAsync(projectId);
            if (project == null) throw new KeyNotFoundException("Projekt nenalezen.");

            var selectedOffers = project.Items
                .Where(i => i.SupplierOffers != null && i.SupplierOffers.Any(o => o.IsSelected))
                .SelectMany(i => i.SupplierOffers
                    .Where(o => o.IsSelected)
                    .Select(o => new { Item = i, Offer = o }))
                .ToList();

            if (!selectedOffers.Any()) throw new InvalidOperationException("Žádné položky k objednání.");

            var sb = new StringBuilder();
            sb.AppendLine("Dodavatel;Součástka;Počet;Cena/ks;Měna;Celkem");

            foreach (var x in selectedOffers)
            {
                var partName = x.Item.Component?.Name ?? x.Item.CustomName ?? "Unknown";
                var total = x.Item.QuantityToBuy * x.Offer.UnitPrice;

                string Escape(string s) => s.Contains(';') ? $"\"{s.Replace("\"", "\"\"")}\"" : s;

                sb.AppendLine($"{Escape(x.Offer.Supplier?.Name ?? "")};{Escape(partName)};{x.Item.QuantityToBuy};{x.Offer.UnitPrice:F2};{x.Offer.Currency};{total:F2}");
            }

            var content = sb.ToString();

            var encoding = new UTF8Encoding(true);
            var preamble = encoding.GetPreamble(); 
            var data = encoding.GetBytes(content); 

            var complete = new byte[preamble.Length + data.Length];
            Buffer.BlockCopy(preamble, 0, complete, 0, preamble.Length);
            Buffer.BlockCopy(data, 0, complete, preamble.Length, data.Length);

            return complete;
        }
    }
}
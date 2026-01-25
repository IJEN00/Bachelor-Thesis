using InventoryApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Services
{
    public class ComponentService : IComponentService
    {
        private readonly AppDbContext _db;
        public ComponentService(AppDbContext db) { _db = db; }


        public async Task AddAsync(Component component)
        {
            _db.Components.Add(component);
            await _db.SaveChangesAsync();
        }


        public async Task DeleteAsync(int id)
        {
            var entity = await _db.Components.FindAsync(id);
            if (entity != null)
            {
                _db.Components.Remove(entity);
                await _db.SaveChangesAsync();
            }
        }


        public async Task<List<Component>> GetAllAsync()
        {
            return await _db.Components.Include(c => c.Documents).Include(c => c.Location).ToListAsync();
        }


        public async Task<Component?> GetByIdAsync(int id)
        {
            return await _db.Components.Include(c => c.Documents).Include(c => c.Location).FirstOrDefaultAsync(c => c.Id == id);
        }


        public async Task<List<Component>> GetLowStockAsync()
        {
            return await _db.Components
                .Include(c => c.Location)
                .Where(c => c.Quantity < (c.ReorderPoint ?? 5))
                .OrderBy(c => c.Quantity)
                .ToListAsync();
        }


        public async Task UpdateAsync(Component component)
        {
            _db.Components.Update(component);
            await _db.SaveChangesAsync();
        }

        public Task<int> GetTotalComponentsAsync()
            => _db.Components.CountAsync();

        public Task<int> GetTotalQuantityAsync()
            => _db.Components.SumAsync(c => c.Quantity);

        public async Task<(List<string> Labels, List<int> Values)> GetConsumptionStatsAsync(int days)
        {
            var since = DateTime.UtcNow.AddDays(-days);

            var consumption = await _db.InventoryTransactions
                .Where(t => t.CreatedAt >= since && t.DeltaQuantity < 0)
                .GroupBy(t => t.ComponentId)
                .Select(g => new
                {
                    ComponentId = g.Key,
                    Used = -g.Sum(x => x.DeltaQuantity) 
                })
                .OrderByDescending(x => x.Used)
                .Take(7) 
                .ToListAsync();

            var componentIds = consumption.Select(x => x.ComponentId).ToList();
            var components = await _db.Components
                .Where(c => componentIds.Contains(c.Id))
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            var labels = new List<string>();
            var values = new List<int>();

            foreach (var item in consumption)
            {
                var name = components.FirstOrDefault(c => c.Id == item.ComponentId)?.Name ?? "Neznámá";
                labels.Add(name);
                values.Add(item.Used);
            }

            return (labels, values);
        }

        public async Task<List<InventoryTransaction>> GetTransactionHistoryAsync(int componentId, int count = 10)
        {
            return await _db.InventoryTransactions
                .Include(t => t.Component) 
                .Where(t => t.ComponentId == componentId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<Component>> FilterComponentsAsync(string? rack, string? drawer, string? box, string? search)
        {
            var query = _db.Components
                .Include(c => c.Location)  
                .Include(c => c.Documents) 
                .AsQueryable();

            if (!string.IsNullOrEmpty(rack))
            {
                query = query.Where(c => c.Location != null && c.Location.Rack == rack);
            }

            if (!string.IsNullOrEmpty(drawer))
            {
                query = query.Where(c => c.Location != null && c.Location.Drawer == drawer);
            }

            if (!string.IsNullOrEmpty(box))
            {
                query = query.Where(c => c.Location != null && c.Location.Box == box);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Name.Contains(search));
            }

            return await query.ToListAsync();
        }

        public async Task AddStockAsync(int id, int amount, string? note)
        {
            if (amount <= 0) throw new ArgumentException("Množství musí být větší než 0.");

            var component = await _db.Components.FindAsync(id);
            if (component == null) throw new Exception("Součástka nenalezena.");

            component.Quantity = (byte)Math.Min(component.Quantity + amount, byte.MaxValue);

            _db.InventoryTransactions.Add(new InventoryTransaction
            {
                ComponentId = id,
                DeltaQuantity = amount,
                Type = InventoryTransactionType.Add,
                Note = note,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }

        public async Task UseStockAsync(int id, int amount, string? note)
        {
            if (amount <= 0) throw new ArgumentException("Množství musí být větší než 0.");

            var component = await _db.Components.FindAsync(id);
            if (component == null) throw new Exception($"Součástka s ID {id} nebyla nalezena.");

            if (component.Quantity < amount)
            {
                throw new InvalidOperationException($"Nelze vyskladnit {amount} ks. Skladem je pouze {component.Quantity} ks.");
            }

            component.Quantity -= (byte)amount;

            _db.InventoryTransactions.Add(new InventoryTransaction
            {
                ComponentId = id,
                DeltaQuantity = -amount,
                Type = InventoryTransactionType.Use,
                Note = note,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }
    }
}

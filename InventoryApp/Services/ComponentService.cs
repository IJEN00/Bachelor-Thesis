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
                .Where(c => c.Quantity <= (c.ReorderPoint ?? 5))
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
    }
}

using InventoryApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Services
{
    public class LocationService : ILocationService
    {
        private readonly AppDbContext _db;
        public LocationService(AppDbContext db)
        {
            _db = db;
        }
        public async Task<IEnumerable<Location>> GetAllAsync()
        {
            return await _db.Locations.ToListAsync();
        }

        public async Task<Location?> GetByIdAsync(int id)
        {
            return await _db.Locations.FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task AddAsync(Location location)
        {
            _db.Locations.Add(location);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _db.Locations.FindAsync(id);
            if (entity != null)
            {
                _db.Locations.Remove(entity);
                await _db.SaveChangesAsync();
            }
        }
    }
}

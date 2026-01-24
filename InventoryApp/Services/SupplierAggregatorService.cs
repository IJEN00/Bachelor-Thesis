using InventoryApp.Models;
using InventoryApp.Services.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Services
{
    public class SupplierAggregatorService
    {
        private readonly AppDbContext _context;
        private readonly IEnumerable<ISupplierClient> _clients;

        public SupplierAggregatorService(
            AppDbContext context,
            IEnumerable<ISupplierClient> clients)
        {
            _context = context;
            _clients = clients;
        }

        public async Task GenerateOffersForProjectAsync(int projectId)
        {
            var project = await _context.Projects
                .Include(p => p.Items)
                    .ThenInclude(i => i.Component)
                .Include(p => p.Items)
                    .ThenInclude(i => i.SupplierOffers)
                        .ThenInclude(o => o.Supplier)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
            {
                throw new InvalidOperationException($"Project {projectId} not found");
            }

            var existingOffers = project.Items.SelectMany(i => i.SupplierOffers).ToList();
            if (existingOffers.Any())
            {
                _context.SupplierOffers.RemoveRange(existingOffers);
                await _context.SaveChangesAsync();
            }

            var itemsToBuy = project.Items
                .Where(i => i.QuantityToBuy > 0)
                .ToList();

            if (!itemsToBuy.Any())
            {
                return;
            }

            var existingSuppliers = await _context.Suppliers.ToListAsync();
            var suppliersByName = existingSuppliers
                .ToDictionary(s => s.Name, s => s, StringComparer.OrdinalIgnoreCase);

            foreach (var client in _clients)
            {
                if (!suppliersByName.ContainsKey(client.SupplierName))
                {
                    var newSupplier = new Supplier
                    {
                        Name = client.SupplierName,
                        WebsiteUrl = client.IsRealApi ? "https://www.tme.eu" : null,
                        HasApi = client.IsRealApi
                    };
                    _context.Suppliers.Add(newSupplier);
                    suppliersByName[client.SupplierName] = newSupplier;
                }
            }

            await _context.SaveChangesAsync();

            foreach (var item in itemsToBuy)
            {
                foreach (var client in _clients)
                {
                    var offers = await client.SearchAsync(item);

                    if (!suppliersByName.TryGetValue(client.SupplierName, out var supplier))
                    {
                        continue;
                    }

                    foreach (var offer in offers)
                    {
                        offer.SupplierId = supplier.Id;
                        offer.ProjectItemId = item.Id;

                        _context.SupplierOffers.Add(offer);
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<SupplierOffer>> SearchForComponentAsync(Component component)
        {
            var dummyItem = new ProjectItem
            {
                Component = component,
                ComponentId = component.Id,
                CustomName = component.Name,
                QuantityToBuy = 1
            };

            var allOffers = new List<SupplierOffer>();

            foreach (var client in _clients)
            {
                try
                {
                    var offers = await client.SearchAsync(dummyItem);

                    foreach (var offer in offers)
                    {
                        offer.Supplier = new Supplier { Name = client.SupplierName };
                        allOffers.Add(offer);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Chyba při hledání u {client.SupplierName}: {ex.Message}");
                }
            }

            return allOffers.OrderBy(o => o.UnitPrice).ToList();
        }
    }
}

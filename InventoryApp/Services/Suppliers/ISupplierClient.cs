using InventoryApp.Models;

namespace InventoryApp.Services.Suppliers
{
    public interface ISupplierClient
    {
        string SupplierName { get; }

        Task<List<SupplierOffer>> SearchAsync(ProjectItem item);

        bool IsRealApi { get; }
    }
}

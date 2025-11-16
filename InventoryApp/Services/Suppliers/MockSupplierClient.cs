using InventoryApp.Models;
using System.Security.Cryptography;

namespace InventoryApp.Services.Suppliers
{
    public class MockSupplierClient : ISupplierClient
    {
        public string SupplierName => "MockSupplier";

        public bool IsRealApi => false;

        public Task<List<SupplierOffer>> SearchAsync(ProjectItem item)
        {
            var qty = item.QuantityToBuy > 0 ? item.QuantityToBuy : item.QuantityRequired;

            decimal basePrice = GetDeterministicPrice(item.Id);

            var offer = new SupplierOffer
            {
                ProjectItemId = item.Id,
                Description = BuildDescription(item),
                UnitPrice = basePrice,
                Currency = "CZK",
                InStock = true,
                MinOrderQty = 1,
                LeadTimeDays = 3,
                ProductUrl = null
            };

            return Task.FromResult(new List<SupplierOffer> { offer });
        }

        private string BuildDescription(ProjectItem item)
        {
            if (item.Component != null)
            {
                return item.Component.Name;
            }

            if (!string.IsNullOrWhiteSpace(item.CustomName))
            {
                return item.CustomName;
            }

            return "Unknown item";
        }

        private decimal GetDeterministicPrice(int key)
        {
            var bytes = BitConverter.GetBytes(key);
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(bytes);
            int value = BitConverter.ToInt32(hash, 0);
            value = Math.Abs(value % 990) + 10; 
            return value / 100m;
        }


    }
}

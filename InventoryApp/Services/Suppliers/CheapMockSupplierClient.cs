using InventoryApp.Models;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace InventoryApp.Services.Suppliers
{
    public class CheapMockSupplierClient : ISupplierClient
    {
        public string SupplierName => "CheapMockSupplier";

        public bool IsRealApi => false;

        public Task<List<SupplierOffer>> SearchAsync(ProjectItem item)
        {
            var qty = item.QuantityToBuy > 0 ? item.QuantityToBuy : item.QuantityRequired;

            decimal basePrice = GetDeterministicPrice(item.Id);

            // tenhle dodavatel je třeba o 15 % levnější
            decimal cheaperPrice = Math.Round(basePrice * 0.85m, 2);

            var offer = new SupplierOffer
            {
                ProjectItemId = item.Id,
                Description = BuildDescription(item),
                UnitPrice = cheaperPrice,
                Currency = "CZK",
                InStock = true,
                MinOrderQty = 1,
                LeadTimeDays = 5,
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
            var bytes = Encoding.UTF8.GetBytes($"cheap-{key}");
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(bytes);
            int value = Math.Abs(BitConverter.ToInt32(hash, 0) % 990) + 10; // 10–999 centů
            return value / 100m;
        }
    }
}

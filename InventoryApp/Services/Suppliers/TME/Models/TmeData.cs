using System.Text.Json.Serialization;

namespace InventoryApp.Services.Suppliers.TME.Models
{
    public class TmeData
    {
        [JsonPropertyName("Currency")]
        public string Currency { get; set; } = "CZK";

        [JsonPropertyName("ProductList")]
        public List<TmeProduct> ProductList { get; set; } = new();
    }
}

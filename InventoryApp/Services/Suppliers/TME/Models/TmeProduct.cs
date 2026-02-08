using System.Text.Json.Serialization;

namespace InventoryApp.Services.Suppliers.TME.Models
{
    public class TmeProduct
    {
        [JsonPropertyName("Symbol")]
        public string Symbol { get; set; } = string.Empty;

        [JsonPropertyName("Producer")]
        public string Producer { get; set; } = string.Empty;

        [JsonPropertyName("Amount")]
        public int Amount { get; set; } 

        [JsonPropertyName("PriceList")]
        public List<TmePrice> PriceList { get; set; } = new();
    }
}

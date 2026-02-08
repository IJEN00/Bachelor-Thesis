using System.Text.Json.Serialization;

namespace InventoryApp.Services.Suppliers.TME.Models
{
    public class TmePrice
    {
        [JsonPropertyName("PriceValue")]
        public decimal PriceValue { get; set; }

        [JsonPropertyName("Amount")]
        public int MinAmount { get; set; }
    }
}

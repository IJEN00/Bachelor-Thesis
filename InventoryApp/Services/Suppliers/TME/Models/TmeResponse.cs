using System.Text.Json.Serialization;

namespace InventoryApp.Services.Suppliers.TME.Models
{
    public class TmeResponse
    {
        [JsonPropertyName("Status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("Data")]
        public TmeData? Data { get; set; }
    }
}

using InventoryApp.Models;
using InventoryApp.Services.Suppliers.Mouser.Models;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Globalization;

namespace InventoryApp.Services.Suppliers.Mouser
{
    public class MouserApiClient : ISupplierClient
    {
        public string SupplierName => "Mouser";
        public bool IsRealApi => true;

        private readonly HttpClient _httpClient;
        private readonly MouserApiOptions _options;

        public MouserApiClient(HttpClient httpClient, IOptions<MouserApiOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<List<SupplierOffer>> SearchAsync(ProjectItem item)
        {
            var offers = new List<SupplierOffer>();

            var c = item.Component;
            string symbol = string.Empty;

            if (c != null && !string.IsNullOrWhiteSpace(c.ManufacturerPartNumber))
            {
                symbol = c.ManufacturerPartNumber;
            }

            if (string.IsNullOrWhiteSpace(symbol) && !string.IsNullOrWhiteSpace(item.CustomName))
            {
                symbol = item.CustomName;
            }

            if (string.IsNullOrWhiteSpace(symbol))
                return offers;

            var requestObj = new MouserSearchRequestRoot
            {
                SearchByPartRequest = new MouserPartSearchRequest
                {
                    MouserPartNumber = symbol,
                    PartSearchOptions = "string" 
                }
            };

            var json = JsonSerializer.Serialize(requestObj);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"{_options.BaseUrl}/search/partnumber?apiKey={_options.ApiKey}";

            var response = await _httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
                return offers;

            var responseJson = await response.Content.ReadAsStringAsync();
            Console.WriteLine("MOUSER RESPONSE:");
            Console.WriteLine(responseJson);

            var data = JsonSerializer.Deserialize<MouserSearchResponse>(responseJson);
            if (data == null || data.SearchResults.Parts.Count == 0)
                return offers;

            var part = data.SearchResults.Parts.First();

            decimal unitPrice = 0m;
            if (part.PriceBreaks != null && part.PriceBreaks.Any())
            {
                var firstBreak = part.PriceBreaks.First();
                var raw = firstBreak.Price ?? "0";

                Console.WriteLine($"MOUSER RAW PRICE: '{raw}'"); 

                raw = raw.Replace("$", "")
                         .Replace("€", "")
                         .Replace("£", "")
                         .Trim();

                if (!decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out unitPrice))
                {
                    decimal.TryParse(raw, NumberStyles.Any, CultureInfo.CurrentCulture, out unitPrice);
                }
            }

            var offer = new SupplierOffer
            {
                ProjectItemId = item.Id,
                Description = part.Description,
                UnitPrice = unitPrice,
                Currency = "CZK", 
                InStock = part.Availability.Contains("Stock"),
                MinOrderQty = part.PriceBreaks.FirstOrDefault()?.Quantity ?? 1,
                LeadTimeDays = null,
                ProductUrl = $"https://www.mouser.com/ProductDetail/{part.MouserPartNumber}"
            };

            offers.Add(offer);
            return offers;
        }
    }
}

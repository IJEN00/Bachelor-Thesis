using InventoryApp.Helpers;
using InventoryApp.Models;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace InventoryApp.Services.Suppliers
{
    public class TMEApiClient : ISupplierClient
    {
        public string SupplierName => "TME";
        public bool IsRealApi => true;

        private readonly HttpClient _httpClient;
        private readonly TMEApiOptions _options;

        public TMEApiClient(HttpClient httpClient, IOptions<TMEApiOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<List<SupplierOffer>> SearchAsync(ProjectItem item)
        {
            var result = new List<SupplierOffer>();

            if (item.Component == null)
                return result;

            if (string.IsNullOrWhiteSpace(_options.BaseUrl) ||
                string.IsNullOrWhiteSpace(_options.Token) ||
                string.IsNullOrWhiteSpace(_options.Secret))
            {
                return result;
            }

            var symbol = BuildSearchQuery(item);
            if (string.IsNullOrWhiteSpace(symbol))
                return result;

            const string endpoint = "Products/GetPrices.json";

            // 1) připravíme parametry BEZ ApiSignature
            var parameters = new Dictionary<string, string>
            {
                ["Language"] = "EN",
                ["SymbolList[0]"] = symbol,
                ["Token"] = _options.Token,
                ["Country"] = "CZ"
            };

            // 2) spočítáme signaturu
            var signature = TMESignatureHelper.CreateSignature(
                _options.BaseUrl,
                endpoint,
                parameters,
                _options.Secret);

            // 3) přidáme ApiSignature jako další parametr
            parameters["ApiSignature"] = signature;

            // 4) pošleme POST jako application/x-www-form-urlencoded
            var content = new FormUrlEncodedContent(parameters);

            try
            {
                var url = $"{_options.BaseUrl.TrimEnd('/')}/{endpoint}";
                var response = await _httpClient.PostAsync(url, content);

                var responseJson = await response.Content.ReadAsStringAsync();

                Console.WriteLine("TME RESPONSE:");
                Console.WriteLine(responseJson);

                if (!response.IsSuccessStatusCode)
                    return result;

                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("Data", out var data) &&
                    data.TryGetProperty("ProductList", out var products) &&
                    products.ValueKind == JsonValueKind.Array)
                {
                    foreach (var p in products.EnumerateArray())
                    {
                        var desc = p.GetPropertyOrDefault("Description", symbol);
                        var producer = p.GetPropertyOrDefault("Producer", "");
                        var productUrl = p.GetPropertyOrDefault("ProductInformationPage", "");

                        decimal unitPrice = 0m;
                        string currency = "CZK";

                        if (p.TryGetProperty("PriceList", out var priceList) &&
                            priceList.ValueKind == JsonValueKind.Array &&
                            priceList.GetArrayLength() > 0)
                        {
                            var firstPrice = priceList[0];
                            unitPrice = firstPrice.GetPropertyOrDefault("PriceValue", 0m);
                            currency = firstPrice.GetPropertyOrDefault("Currency", "CZK");
                        }

                        var offer = new SupplierOffer
                        {
                            Description = $"{producer} {desc}".Trim(),
                            UnitPrice = unitPrice,
                            Currency = currency,
                            InStock = true, // GetStocks 
                            MinOrderQty = 1,
                            LeadTimeDays = null,
                            ProductUrl = string.IsNullOrWhiteSpace(productUrl) ? null : productUrl
                        };

                        result.Add(offer);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TME API error: {ex.Message}");
            }

            return result;
        }

        private string BuildSearchQuery(ProjectItem item)
        {
            if (item.Component == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(item.Component.ManufacturerPartNumber))
                return item.Component.ManufacturerPartNumber;

            return item.Component.Name;
        }
    }
}

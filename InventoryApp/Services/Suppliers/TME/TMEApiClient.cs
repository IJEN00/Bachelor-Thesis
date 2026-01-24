using InventoryApp.Models;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace InventoryApp.Services.Suppliers.TME
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

        private async Task<string?> ResolveTmeSymbolAsync(ProjectItem item)
        {
            var searchPlain = BuildSearchQuery(item);
            if (string.IsNullOrWhiteSpace(searchPlain))
                return null;

            if (string.IsNullOrWhiteSpace(_options.BaseUrl) ||
                string.IsNullOrWhiteSpace(_options.Token) ||
                string.IsNullOrWhiteSpace(_options.Secret))
            {
                return null;
            }

            const string searchEndpoint = "Products/Search.json";

            var parameters = new Dictionary<string, string>
            {
                ["Language"] = "EN",
                ["Country"] = "CZ",
                ["SearchPlain"] = searchPlain,
                ["Token"] = _options.Token
            };

            var signature = TMESignatureHelper.CreateSignature(
                _options.BaseUrl,
                searchEndpoint,
                parameters,
                _options.Secret);

            parameters["ApiSignature"] = signature;

            var content = new FormUrlEncodedContent(parameters);
            var url = $"{_options.BaseUrl.TrimEnd('/')}/{searchEndpoint}";

            try
            {
                var response = await _httpClient.PostAsync(url, content);
                var json = await response.Content.ReadAsStringAsync();

                Console.WriteLine("TME SEARCH RESPONSE:");
                Console.WriteLine(json);

                if (!response.IsSuccessStatusCode)
                    return null;

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("Data", out var data) ||
                    !data.TryGetProperty("ProductList", out var products) ||
                    products.ValueKind != JsonValueKind.Array ||
                    products.GetArrayLength() == 0)
                {
                    return null;
                }

                JsonElement? best = null;

                var manufacturer = item.Component?.Manufacturer;
                if (!string.IsNullOrWhiteSpace(manufacturer))
                {
                    foreach (var p in products.EnumerateArray())
                    {
                        if (p.TryGetProperty("Producer", out var prodElem) &&
                            prodElem.ValueKind == JsonValueKind.String &&
                            string.Equals(prodElem.GetString(), manufacturer,
                                          StringComparison.OrdinalIgnoreCase))
                        {
                            best = p;
                            break;
                        }
                    }
                }

                if (best == null)
                {
                    best = products.EnumerateArray().First();
                }

                if (best.Value.TryGetProperty("Symbol", out var symbolElem) &&
                    symbolElem.ValueKind == JsonValueKind.String)
                {
                    var symbol = symbolElem.GetString();
                    Console.WriteLine($"TME: pro '{searchPlain}' nalezen symbol '{symbol}'");
                    return symbol;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TME SEARCH error: {ex.Message}");
                return null;
            }
        }

        public async Task<List<SupplierOffer>> SearchAsync(ProjectItem item)
        {
            var result = new List<SupplierOffer>();

            var symbol = await ResolveTmeSymbolAsync(item);
            if (string.IsNullOrWhiteSpace(symbol))
                return result;

            if (string.IsNullOrWhiteSpace(_options.BaseUrl) ||
                string.IsNullOrWhiteSpace(_options.Token) ||
                string.IsNullOrWhiteSpace(_options.Secret))
            {
                return result;
            }

            const string endpoint = "Products/GetPricesAndStocks.json";

            var parameters = new Dictionary<string, string>
            {
                ["Language"] = "EN",
                ["SymbolList[0]"] = symbol,
                ["Token"] = _options.Token,
                ["Country"] = "CZ"
            };

            var signature = TMESignatureHelper.CreateSignature(
                _options.BaseUrl,
                endpoint,
                parameters,
                _options.Secret);

            parameters["ApiSignature"] = signature;

            var content = new FormUrlEncodedContent(parameters);
            var url = $"{_options.BaseUrl.TrimEnd('/')}/{endpoint}";

            try
            {
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
                    foreach (var product in products.EnumerateArray())
                    {
                        int stockQty = 0;
                        if (product.TryGetProperty("Amount", out var amountElem) &&
                            amountElem.ValueKind == JsonValueKind.Number &&
                            amountElem.TryGetInt32(out var a))
                        {
                            stockQty = a;
                        }

                        var requiredQty = item.QuantityToBuy > 0 ? item.QuantityToBuy : 1;

                        if (stockQty < requiredQty)
                        {
                            Console.WriteLine($"TME: Požadovaný počet není skladem.");
                            continue;
                        }

                        decimal unitPrice = 0m;
                        string currency = data.GetProperty("Currency").GetString() ?? "CZK";

                        if (product.TryGetProperty("PriceList", out var priceListElem) &&
                            priceListElem.ValueKind == JsonValueKind.Array &&
                            priceListElem.GetArrayLength() > 0)
                        {
                            var firstPrice = priceListElem[0];
                            if (firstPrice.TryGetProperty("PriceValue", out var priceElem) &&
                                priceElem.ValueKind == JsonValueKind.Number &&
                                priceElem.TryGetDecimal(out var p))
                            {
                                unitPrice = p;
                            }
                        }

                        var desc = product.GetProperty("Symbol").GetString() ?? symbol;

                        var offer = new SupplierOffer
                        {
                            ProjectItemId = item.Id,
                            Description = desc,
                            UnitPrice = unitPrice,
                            Currency = currency,
                            InStock = stockQty > 0,
                            MinOrderQty = 1,
                            LeadTimeDays = null,
                            ProductUrl = $"https://www.tme.eu/cz/details/{desc}"
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
            if (item.Component != null && !string.IsNullOrWhiteSpace(item.Component.ManufacturerPartNumber))
            {
                return item.Component.ManufacturerPartNumber;
            }

            if (!string.IsNullOrWhiteSpace(item.CustomName))
            {
                return item.CustomName;
            }

            return string.Empty;
        }
    }
}

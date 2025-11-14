using System.Security.Cryptography;
using System.Text;

namespace InventoryApp.Helpers
{
    public static class TMESignatureHelper
    {
        public static string CreateSignature(
        string baseUrl,
        string endpointPath,           // např. "Products/GetPrices.json"
        IDictionary<string, string> parameters,
        string secretKey)
        {
            // 1) URL bez parametrů
            var fullUrl = $"{baseUrl.TrimEnd('/')}/{endpointPath.TrimStart('/')}";

            // 2) parametry seřadit podle názvu
            var sorted = parameters
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .ToList();

            // 3) poskládat param string: key=val&key=val...
            var paramString = string.Join("&", sorted.Select(kv =>
                $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

            // 4) základ pro signaturu:
            //    POST&url-encoded(fullUrl)&url-encoded(paramString)
            var method = "POST";
            var baseString = $"{method}&{Uri.EscapeDataString(fullUrl)}&{Uri.EscapeDataString(paramString)}";

            // 5) HMAC-SHA1 + Base64
            using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(secretKey));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(baseString));
            var signature = Convert.ToBase64String(hashBytes);

            return signature;
        }
    }
}

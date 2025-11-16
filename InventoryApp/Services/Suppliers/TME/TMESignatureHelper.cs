using System.Security.Cryptography;
using System.Text;

namespace InventoryApp.Services.Suppliers.TME
{
    public static class TMESignatureHelper
    {
        public static string CreateSignature(
        string baseUrl,
        string endpointPath,           
        IDictionary<string, string> parameters,
        string secretKey)
        {
            var fullUrl = $"{baseUrl.TrimEnd('/')}/{endpointPath.TrimStart('/')}";

            var sorted = parameters
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .ToList();

            var paramString = string.Join("&", sorted.Select(kv =>
                $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

            var method = "POST";
            var baseString = $"{method}&{Uri.EscapeDataString(fullUrl)}&{Uri.EscapeDataString(paramString)}";

            using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(secretKey));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(baseString));
            var signature = Convert.ToBase64String(hashBytes);

            return signature;
        }
    }
}

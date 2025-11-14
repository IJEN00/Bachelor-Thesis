using System.Text.Json;

namespace InventoryApp.Helpers
{
    public static class JsonExtension
    {
        public static string GetPropertyOrDefault(this JsonElement element, string propertyName, string defaultValue)
        {
            if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
                return prop.GetString() ?? defaultValue;

            return defaultValue;
        }

        public static decimal GetPropertyOrDefault(this JsonElement element, string propertyName, decimal defaultValue)
        {
            if (element.TryGetProperty(propertyName, out var prop) &&
                prop.ValueKind == JsonValueKind.Number &&
                prop.TryGetDecimal(out var value))
            {
                return value;
            }

            return defaultValue;
        }
    }
}

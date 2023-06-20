using System.Text.Json;

namespace ibxdocparser
{
    public static class JsonExtensions
    {
        public static JsonElement? GetPropertyIfExists(this JsonElement element, string propertyName) =>
            element.TryGetProperty(propertyName, out var property) ? property : null;

        public static JsonElement? GetDescendant(this JsonElement element, string path) =>
            path.Split('.').Aggregate<string, JsonElement?>(element, (acc, x) =>
            acc?.TryGetProperty(x, out var property) == true ? property : null);

        public static IEnumerable<(JsonElement Element, string PropertyName)> GetPropertiesWhere(this JsonElement element, Func<JsonElement, string, bool> filter) =>
            element.EnumerateObject().Where(y => filter(y.Value, y.Name)).Select(z => (Element: z.Value, PropertyName: z.Name));

        public static string? GetPropertyString(this JsonElement element, string propertyName) =>
            element.TryGetProperty(propertyName, out var x) ? x.GetString() : null;

        public static string? GetDescendantString(this JsonElement element, string path) =>
            element.GetDescendant(path)?.GetString();

        public static double? GetPropertyDouble(this JsonElement element, string propertyName) =>
            element.TryGetProperty(propertyName, out var x) ? x.TryGetDouble(out var y) ? y : null : null;

        public static double? GetPropertyLong(this JsonElement element, string propertyName) =>
            element.TryGetProperty(propertyName, out var x) ? x.TryGetUInt64(out var y) ? y : null : null;

        internal static Location ParseLocation(this JsonElement el) =>
           new(
               Name: el.GetPropertyString("name"),
               Phone: el.GetPropertyString("phone"),
               Latitude: el.GetPropertyDouble("latitude") ?? 0,
               Longitude: el.GetPropertyDouble("longitude") ?? 0,
               InNetwork: el.TryGetProperty("inNetwork", out var inNetwork)
                   ? inNetwork.GetBoolean() : null,
               Address: el.TryGetProperty("address", out var addressNode)
                   ? new Address(
                       addressNode.GetPropertyString("line1") ?? "",
                       addressNode.GetPropertyString("line2") ?? "",
                       addressNode.GetPropertyString("city") ?? "",
                       addressNode.GetPropertyString("state") ?? "",
                       addressNode.GetPropertyString("zip") ?? "")
                   : null
            );

        internal static Location[] ParseLocations(this JsonElement el) =>
            el
              .EnumerateArray()
              .Select(ParseLocation)
              .Where(x => !string.IsNullOrEmpty(x.Name) || !string.IsNullOrEmpty(x.Address?.Line1))
              .DistinctBy(h => h.ToString().ToUpper())
              .ToArray()
           ?? Array.Empty<Location>();
    }

}


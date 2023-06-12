using System.Text.Json;
using System.Text.RegularExpressions;

namespace ibxdocparser
{
    internal interface IJsonParser<T>
    {
        public T Parse(string json);
    }

    internal record IbxProfile
    {
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? FullName { get; set; }
        public long? Id { get; set; }
        public string? Gender { get; set; }
        public string? BoardCertified { get; set; }
        public string? Education { get; set; }
        public string? Residency { get; set; }
        public string? ImageUri { get; set; }
        public string[] GroupAffiliations { get; set; } = Array.Empty<string>();
        public string[] HospitalAffiliations { get; set; } = Array.Empty<string>();
        public Location[] Locations { get; set; } = Array.Empty<Location>();
    }

    internal class IbxProfileJsonParser : IJsonParser<IbxProfile>
    {
        public IbxProfile Parse(string json)
        {
            var parsingOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            JsonElement rootElement = JsonSerializer.Deserialize<JsonElement>(json, parsingOptions);
            var attr = new Dictionary<string, (string? Name, JsonElement? Value)>();


            Dictionary<string, (string? Name, JsonElement? Value)> GetAttributes(JsonElement parent)
            {
                var attrs = new Dictionary<string, (string? Name, JsonElement? Value)>();
                var attributeElements = parent.GetPropertyIfExists("attributes");
                if (!attributeElements.HasValue) return attrs;

                foreach (var el in attributeElements.Value.EnumerateArray())
                {
                    var key = el.GetPropertyString("key");
                    if (key is null) continue;
                    attrs[key.ToUpper()] = (el.GetPropertyString("name"), el.GetPropertyIfExists("value"));
                }

                return attrs;
            }

            static JsonElement? GetAttributeValue(Dictionary<string, (string? Name, JsonElement? Value)> attrs, string key) =>
                attrs.TryGetValue(key.ToUpper(), out var x) ? x.Value : null;

            var providerAttrs = GetAttributes(rootElement)!;


            string BoardCertification()
            {

                try
                {
                    if (providerAttrs is null) return "";
                    var valueEl = GetAttributeValue(providerAttrs, "BOARD_CERTIFICATION")?.GetDescendant("boardCertification");
                    return !valueEl.HasValue ? "" : string.Join(", ", valueEl.Value.EnumerateArray().Select(e => e.GetString()));
                }
                catch (Exception exc)
                {
                    return "";
                }
            }

            try
            {
                return new IbxProfile()
                {
                    FirstName = rootElement.GetDescendantString("provider.firstName"),
                    MiddleName = rootElement.GetDescendantString("provider.middleName"),
                    LastName = rootElement.GetDescendantString("provider.lastName"),
                    FullName = rootElement.GetDescendantString("provider.fullName"),
                    ImageUri = rootElement.GetDescendantString("photoPath"),
                    Id = rootElement.GetPropertyIfExists("id")?.GetInt64(),
                    Gender = GetAttributeValue(providerAttrs, "GENDER")?.GetString(),
                    BoardCertified = BoardCertification(),
                    Education = GetAttributeValue(providerAttrs, "EDUCATION")
                   ?.EnumerateArray()
                   .Where(x => Regex.IsMatch(x.GetPropertyString("code") ?? "", "^(MD|DO)$", RegexOptions.IgnoreCase))
                   .Select(x => x.GetPropertyString("institution"))
                   .FirstOrDefault(),

                    Residency =
                 GetAttributeValue(providerAttrs, "EDUCATION")?.EnumerateArray()
                     .Where(x => Regex.IsMatch(x.GetPropertyString("code") ?? "", "^Residency$", RegexOptions.IgnoreCase))
                     .Select(x => x.GetPropertyString("institution"))
                     .FirstOrDefault(),

                    GroupAffiliations =
                   GetAttributeValue(providerAttrs, "GROUP_AFFILIATIONS")
                       ?.EnumerateArray()
                       .Select(x => x.GetPropertyString("name") ?? "")
                       .Where(x => x.Length > 0)
                       .ToArray() ?? Array.Empty<string>(),

                    Locations =
                   rootElement.GetPropertyIfExists("locations")
                   ?.EnumerateArray()
                   .Select(l =>
                       new Location()
                       {
                           Address = new Address()
                           {
                               Line1 = l.GetDescendantString("address.line1"),
                               Line2 = l.GetDescendantString("address.line2"),
                               City = l.GetDescendantString("address.city"),
                               State = l.GetDescendantString("address.state"),
                               Country = l.GetDescendantString("address.country"),
                               Zip = l.GetDescendantString("address.zip"),
                               County = l.GetDescendantString("address.county"),
                           },
                           Latitude = l.GetPropertyDouble("latitude"),
                           Longitude = l.GetPropertyDouble("longitude"),
                           Name = l.GetPropertyString("name") ?? "",
                           Phone = l.GetPropertyString("phone") ?? ""
                       }
                   )
                   .ToArray()
                   ?? Array.Empty<Location>(),

                    HospitalAffiliations =
                   GetAttributeValue(providerAttrs, "HOSPITAL_AFFILIATIONS")
                       ?.EnumerateArray()
                       .Select(x => x.GetPropertyString("name") ?? "")
                       .Where(x => x.Length > 0)
                       .ToArray() ?? Array.Empty<string>()
                };
            }
            catch (Exception)
            {
                var rawText = rootElement.GetRawText();
                throw;
            }

        }
    }
}
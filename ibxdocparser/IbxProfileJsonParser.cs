using System.Text.Json;

namespace ibxdocparser
{
    internal interface IJsonParser<T>
    {
        public T Parse(string json);
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


            var education =
                GetAttributeValue(providerAttrs, "EDUCATION")
                  ?.EnumerateArray()
                  .Where(x => !x.GetPropertyString("code")?.Equals("Residency", StringComparison.CurrentCultureIgnoreCase) == true)
                  .Select(x => new Experience(
                          x.GetPropertyString("code") ?? "",
                          x.GetPropertyString("institution") ?? "",
                          int.TryParse(x.GetPropertyString("year"), out var year) ? year : null))
                  .DistinctBy(x => x.ToString().ToUpper())
                  .ToArray()
                ?? Array.Empty<Experience>();

            var residency =
                GetAttributeValue(providerAttrs, "EDUCATION")
                  ?.EnumerateArray()
                  .Where(x => x.GetPropertyString("code")?.Equals("Residency", StringComparison.CurrentCultureIgnoreCase) == true)
                  .Select(x => new Experience(
                          x.GetPropertyString("code") ?? "",
                          x.GetPropertyString("institution") ?? "",
                          int.TryParse(x.GetPropertyString("year"), out var year) ? year : null))
                  .DistinctBy(x => x.ToString().ToUpper())
                  .ToArray()
                ?? Array.Empty<Experience>();

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
                    Education = education,
                    Residencies = residency,
                    GroupAffiliations =
                        GetAttributeValue(providerAttrs, "GROUP_AFFILIATIONS")
                        ?.ParseLocations()
                        ?? Array.Empty<Location>(),
                    Locations =
                        rootElement.GetPropertyIfExists("locations")
                        ?.ParseLocations()
                        ?? Array.Empty<Location>(),
                    HospitalAffiliations =
                        GetAttributeValue(providerAttrs, "HOSPITAL_AFFILIATIONS")
                        ?.ParseLocations()
                        ?? Array.Empty<Location>()
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
using HtmlAgilityPack;
using System.Diagnostics;

namespace ibxdocparser
{
    internal class LvhnSummaryNodeParser
    {
        private readonly HtmlNode _resultNode;
        private readonly Uri _listingSource;

        public LvhnSummaryNodeParser(string html, Uri listingSource)
        {
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            _resultNode = doc.DocumentNode;
            _listingSource = listingSource;
        }

        public LvhnDocSummary Parse()
        {
            var result = new LvhnDocSummary(
                ParseName(),
                ParseDetailsLink(),
                ParseSpecialties(),
                ParseAreasOfFocus(),
                ParseLocation(),
                ParseImageUri(),
                ParseAcceptingNewPatients());
            return result;
        }


        private bool ParseAcceptingNewPatients() =>
            _resultNode.SelectSingleNode($"//*[{Utilities.XpathAttrContains("accepting-new-patients")}]") is not null;

        private Uri? ParseImageUri()
        {
            string? relativeSource = _resultNode.SelectSingleNode($"//div[{Utilities.XpathAttrContains("headshot")}]//img")
                ?.GetAttributeValue("src", "");

            if (string.IsNullOrEmpty(relativeSource)) return null;
            try
            {
                return new Uri(_listingSource, relativeSource);
            }
            catch (Exception)
            {
                Debug.WriteLine("Error parsing image source: " + relativeSource);
                return null;
            }
        }

        private string ParseName()
        {
            var node = _resultNode.SelectSingleNodeWithClass("field--name-node-title", "div");
            var name = node?.GetEscapedInnerText() ?? "";
            return name;
        }


        private Uri ParseDetailsLink()
        {
            string href = _resultNode
                .SelectSingleNodeWithClass("field--name-node-title", "div")
                ?.SelectSingleNode(".//a")
                ?.GetAttributeValue("href", "")
                ?? throw new Exception("Link not found.");
            return new Uri(_listingSource, href);
        }

        private Location? ParseLocation()
        {
            var locationNode = _resultNode.SelectSingleNodeWithClass("node--type-location", "div");
            return locationNode is null
                ? null
                : locationNode is not null ? new Location()
                {
                    Address = new()
                    {
                        City = locationNode.SelectSingleNodeWithClass("locality", "span")?.GetEscapedInnerText() ?? "",
                        State = locationNode.SelectSingleNodeWithClass("administrative-area", "span")?.GetEscapedInnerText() ?? "",
                        Line1 = locationNode.SelectSingleNodeWithClass("address-line1", "span")?.GetEscapedInnerText() ?? "",
                        Line2 = locationNode.SelectSingleNodeWithClass("address-line2", "span")?.GetEscapedInnerText() ?? "",
                        County = "",
                        Country = locationNode.SelectSingleNodeWithClass("country", "span")?.GetEscapedInnerText() ?? "",
                        Zip = locationNode.SelectSingleNodeWithClass("postal-code", "span")?.GetEscapedInnerText() ?? "",
                    },
                    Name = locationNode.SelectSingleNodeWithClass("field-name-node-title")?.GetEscapedInnerText() ?? "",
                    Phone = locationNode.SelectSingleNodeWithClass("field--name-field-phone", "div")?.GetEscapedInnerText() ?? "",
                } : null;
        }

        public string[] ParseAreasOfFocus() =>
            (_resultNode.SelectNodes($".//div[{Utilities.XpathAttrContains("highlights")}]/h4")
                .FirstOrDefault(n => n.GetEscapedInnerText().Contains("Area of Focus", StringComparison.CurrentCultureIgnoreCase))
                ?.NextSiblingElements()
                .FirstOrDefault(n => n.Name == "ul")
                .SelectNodes(".//li")
                .Select(n => n.GetEscapedInnerText())
                .ToArray()) ?? Array.Empty<string>();

        public string[] ParseSpecialties() =>
            (_resultNode.SelectNodes($".//div[{Utilities.XpathAttrContains("highlights")}]/h4")
                .FirstOrDefault(n => n.GetEscapedInnerText().Contains("Specialties", StringComparison.CurrentCultureIgnoreCase))
                ?.NextSiblingElements()
                .FirstOrDefault(n => n.Name == "ul")
                .SelectNodes(".//li")
                .Select(n => n.GetEscapedInnerText())
                .ToArray()) ?? Array.Empty<string>();
    }
}

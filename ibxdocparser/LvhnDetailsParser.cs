using HtmlAgilityPack;

namespace ibxdocparser
{
    internal class LvhnDetailsParser
    {
        private readonly HtmlNode _node;
        private readonly Uri _source;

        public LvhnDetailsParser(string html, Uri source)
        {
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            _node = doc.DocumentNode;
            _source = source;
        }

        private HtmlNode? _historyNodeValue = null;
        private HtmlNode? _historyNode
        {
            get
            {
                _historyNodeValue ??= _node.SelectSingleNodeWithClass("history", "div"); ;
                return _historyNodeValue;
            }
        }

        public static HtmlNode? GetSubsection(HtmlNode sectionNode, string label)
        {
            var header = sectionNode
                .SelectNodes(".//h3")
                .Where(n => n.GetEscapedInnerText().Equals(label, StringComparison.CurrentCultureIgnoreCase))
                .FirstOrDefault();

            foreach (var sibling in header?.NextSiblingElements() ?? Enumerable.Empty<HtmlNode>())
            {
                if (sibling.Name == "h3")
                {
                    return null;
                }
                if (sibling.GetAttributeValue("class", "").Contains("body"))
                {
                    return sibling;
                }
            }
            return null;
        }

        public static (string Title, string Value)[] GetSubsectionListings(HtmlNode subsectionNode)
        {
            var result = subsectionNode
                .SelectNodes(".//p")
                .Select(n =>
                {
                    string title = n.SelectSingleNode(".//strong")
                        ?.GetEscapedInnerText()
                        ?? "";

                    string innerText = n?.ChildNodes
                        .Where(n => n.NodeType == HtmlNodeType.Text)
                        .Select(n => n.GetEscapedInnerText())
                        .FirstOrDefault()
                        ?? "";

                    return (Title: title, Value: innerText);
                })
                .Where(x => x.Title.Length > 0 || x.Value.Length > 0)
                .ToArray();
            return result;
        }




        private string ParseBioDescription() =>
            _node.SelectSingleNodeWithClass("doctor-bio")?.GetEscapedInnerText() ?? "";

        /// <summary>
        /// The label of the history type, either
        /// </summary>
        /// <param name="historyType">The label of the type, e.g. Education, Training, or Certifications.</param>
        /// <returns></returns>
        private Experience[] ParseHistory(string historyType)
        {
            if (_historyNode is null) goto NotFound;
            var subsection = GetSubsection(_historyNode, historyType);
            if (subsection is null) goto NotFound;
            var listings = GetSubsectionListings(subsection).ToList();

            static (string stripped, int? year) ExtractYear(string str)
            {
                var yearMatch = System.Text.RegularExpressions.Regex.Match(str, @"\d{4}");
                return yearMatch.Success
                    ? (str.Replace(yearMatch.Value, "").Trim('\r', '\n', ',', '.', ':', ' ', '\t'), int.Parse(yearMatch.Value))
                    : (str, null);
            }

            return listings
                .Select(x =>
                {
                    (string strippedValue, int? valueYear) = ExtractYear(x.Value);
                    (string strippedTitle, int? titleYear) = ExtractYear(x.Title);

                    int? year = valueYear ?? titleYear;
                    string type = strippedTitle;
                    string institution = strippedValue;

                    string[] valueParts = strippedValue.Split(",").Select(x => x.Trim()).ToArray();
                    if (valueParts.Length > 1)
                    {
                        institution = valueParts[0];
                        type = $"{type} ({valueParts[1]})";
                    }

                    return new Experience(type, institution, year);
                })
                .ToArray();

        NotFound:
            return Array.Empty<Experience>();
        }

        private Uri? ParseScholarlyWorksLink()
        {
            try
            {
                string? href = _node.SelectSingleNode($"//div[{Utilities.XpathAttrContains("field-name-field-has-scholarly-works")}]//a")?.GetAttributeValue("href", "");
                return Uri.IsWellFormedUriString(href, UriKind.Absolute) ? new Uri(href) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string[] ParseConditionsTreated() => _node
                .SelectSingleNode($"//ul[{Utilities.XpathAttrContains("full")} and @aria-describedby='conditions-label']")
                ?.SelectNodes(".//li")
                ?.Select(x => x.GetEscapedInnerText())
                .Where(s => s.Length > 0)
                .ToArray()
            ?? Array.Empty<string>();
        private string[] ParseServicesOffered() => _node
                .SelectSingleNode($"//ul[{Utilities.XpathAttrContains("full")} and @aria-describedby='services-label']")
                ?.SelectNodes(".//li")
                ?.Select(x => x.GetEscapedInnerText())
                .Where(s => s.Length > 0)
                .ToArray()
            ?? Array.Empty<string>();

        public LvhnDocDetails Parse()
        {
            var result = new LvhnDocDetails(
                ParseBioDescription(),
                ParseHistory("Education"),
                ParseHistory("Training"),
                ParseHistory("Certifications"),
                ParseScholarlyWorksLink(),
                ParseConditionsTreated(),
                ParseServicesOffered());

            return result;
        }

    }
}

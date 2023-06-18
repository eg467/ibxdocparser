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
                .SelectNodes("//h3")
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
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(subsectionNode.OuterHtml);
            var result = doc.DocumentNode
                .SelectNodes("//p")
                .Select(n =>
                {
                    string title = n.SelectSingleNode("//strong")
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
            return listings
                .Select(x =>
                {
                    var yearMatch = System.Text.RegularExpressions.Regex.Match(x.Value, @"\d{4}$");
                    int? year = yearMatch.Success ? int.Parse(yearMatch.Value) : null;
                    string description = yearMatch.Success
                        ? x.Value.Replace(yearMatch.Value, "").Trim()
                        : x.Value;
                    return new Experience(x.Title, description, year);
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
                .SelectNodes($"//ul[{Utilities.XpathAttrContains("full")} and @aria-describedby='conditions-label']/li")
                ?.Select(x => x.GetEscapedInnerText())
                .Where(s => s.Length > 0)
                .ToArray()
            ?? Array.Empty<string>();
        private string[] ParseServicesOffered() => _node
                .SelectNodes($"//ul[{Utilities.XpathAttrContains("full")} and @aria-describedby='services-label']/li")
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

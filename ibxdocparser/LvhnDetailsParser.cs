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
        private Experience[] ParseHistory(string historyType, bool splitByComma = true)
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
                    string details = strippedValue;

                    if (splitByComma)
                    {
                        string[] valueParts = strippedValue.Split(",").Select(x => x.Trim()).ToArray();
                        if (valueParts.Length > 1)
                        {
                            institution = valueParts[0];
                            details = valueParts[1];
                        }
                        else
                        {
                            details = "";
                        }
                    }

                    return new Experience(type, details, institution, year);
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

        private Rating[] ParseRatings()
        {
            var ratingsContainer = _node.SelectSingleNode($".//div[{Utilities.XpathAttrContains("ds-breakdown")}]");

            if (ratingsContainer is null)
            {
                return Array.Empty<Rating>();
            }

            // Parse the overall rating
            var ratings = ratingsContainer.ChildNodes
                .Where(n => n.Name == "div")
                .Select(n =>
                    {
                        var text = n.InnerText.Trim();
                        var pattern = @"(?<average>[\d\.]+) out of (?<max>\d+)[\D]+(?<count>\d+) Ratings";
                        var match = System.Text.RegularExpressions.Regex.Match(text, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (!match.Success)
                        {
                            return null;
                        }

                        var average = double.Parse(match.Groups["average"].Value);
                        var maxRating = int.Parse(match.Groups["max"].Value);
                        var count = int.Parse(match.Groups["count"].Value);
                        return new Rating(average, maxRating, "Overall", RatingSource.Lvhn, count);
                    })
                .Where(x => x is not null)
                .Cast<Rating>()
                .ToList();


            // Parse ratings by category
            IEnumerable<Rating> categoryRatings = ratingsContainer.SelectNodes($".//li")?.Select(li =>
            {
                var descriptionWithCount = li.SelectSingleNode($"./span[{Utilities.XpathAttrContains("ds-questiontext")}]")?.InnerText.Trim() ?? "";
                var pattern = @"^(?<description>[^\(]+)\s*\((?<count>\d+)\)|^(?<description>[^\(]+)$";
                var match = System.Text.RegularExpressions.Regex.Match(descriptionWithCount, pattern);
                var description = match.Groups["description"].Success ? match.Groups["description"].Value.Trim() : "";
                var count = match.Groups["count"].Success ? int.Parse(match.Groups["count"].Value) : -1;
                var ratingStr = li.SelectSingleNode($"./span[{Utilities.XpathAttrContains("ds-average")}]")?.InnerText.Trim() ?? "";
                var rating = double.TryParse(ratingStr, out var parsedRating) ? parsedRating : -1;
                var maxRating = 5;
                return new Rating(rating, maxRating, description, RatingSource.Lvhn, count);
            }) ?? Enumerable.Empty<Rating>();

            ratings.AddRange(categoryRatings);
            return ratings.ToArray();
        }

        private string[] ParseConditionsTreated() => _node
                .SelectSingleNode($".//ul[{Utilities.XpathAttrContains("full")} and @aria-describedby='conditions-label']")
                ?.SelectNodes(".//li")
                ?.Select(x => x.GetEscapedInnerText())
                .Where(s => s.Length > 0)
                .ToArray()
            ?? Array.Empty<string>();
        private string[] ParseServicesOffered() => _node
                .SelectSingleNode($".//ul[{Utilities.XpathAttrContains("full")} and @aria-describedby='services-label']")
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
                ParseHistory("Certifications", false),
                ParseScholarlyWorksLink(),
                ParseConditionsTreated(),
                ParseServicesOffered(),
                ParseRatings());

            return result;
        }

    }
}

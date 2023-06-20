using HtmlAgilityPack;
using System.Diagnostics;
using System.Web;

namespace ibxdocparser
{
    internal class LvhnOrgHtmlParser
    {
        public LvhnOrgHtmlParser() { }

        /// <summary>
        /// Sets the desired page number ro search in the Uri
        /// </summary>
        /// <param name="source"></param>
        /// <param name="pageNum"></param>
        /// <returns></returns>
        private static Uri SetPageNumber(Uri source, int pageNum)
        {
            var query = HttpUtility.ParseQueryString(source.Query);
            query.Set("page", pageNum.ToString());
            var builder = new UriBuilder(source)
            {
                Query = query.ToString()
            };
            return builder.Uri;
        }

        /// <summary>
        /// Retrieves and parses both listing summaries and details for each result.
        /// </summary>
        /// <returns></returns>
        public static async Task<List<LvhnProfile>> ParseFullResultsAsync(Uri listingUrl)
        {
            List<LvhnProfile> results = new();
            List<LvhnDocSummary> summaries = await ParseDocSummariesAsync(listingUrl);
            int i = 0;
            foreach (var summary in summaries)
            {
                Debug.WriteLine($"Downloading {summary.Name} ({++i}/{summaries.Count})");
                LvhnDocDetails? details = null;
                string? error = null;
                try
                {
                    details = await ParseDocDetailsAsync(summary.DetailsUri);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error parsing details at ({summary.DetailsUri}) ({ex})");
                    error = ex.Message;
                    throw;

                }
                var result = new LvhnProfile(summary, details, error);
                results.Add(result);
            }

            return results;
        }

        public static async Task<List<LvhnDocSummary>> ParseDocSummariesAsync(Uri listingUrl)
        {
            List<LvhnDocSummary> allSummaries = new();

            for (var page = 0; true; page++)
            {
                Debug.WriteLine($"Parsing results for page {page + 1}");
                Uri pageUri = SetPageNumber(listingUrl, page);
                string html = await Utilities.DownloadHtmlAsync(pageUri);
                var parser = new LvhnSummaryNodesParser(html, pageUri);
                List<LvhnDocSummary> summariesForPage = parser.Parse().ToList();
                if (!summariesForPage.Any()) break;
                allSummaries.AddRange(summariesForPage);
            }
            return allSummaries;
        }

        public static async Task<LvhnDocDetails> ParseDocDetailsAsync(Uri listingUrl)
        {
            string html = await Utilities.DownloadHtmlAsync(listingUrl);
            var parser = new LvhnDetailsParser(html, listingUrl);
            var result = parser.Parse();
            return result;
        }
    }

    public static class WebAgilityPackExtensions
    {

        private const string ClassSelector = "//{0}[contains(@class, '{1}')]";

        public static HtmlNode? SelectSingleNodeWithClass(this HtmlNode node, string className, string tagName = "*")
        {
            var result = node.SelectSingleNode(string.Format(ClassSelector, tagName, className));
            return result;
        }

        public static IEnumerable<HtmlNode> NextSiblingElements(this HtmlNode node)
        {
            var n = node.NextSibling;
            while (n is not null)
            {
                if (n.NodeType == HtmlNodeType.Element)
                {
                    yield return n;
                }
                n = n.NextSibling;
            }
        }

        public static string GetEscapedInnerText(this HtmlNode node) =>
            HttpUtility.HtmlDecode(node?.InnerText?.Trim() ?? string.Empty);


        public static HtmlNodeCollection SelectNodesWithClass(this HtmlNode node, string className, string tagName = "*") =>
            node.SelectNodes(string.Format(ClassSelector, tagName, className));
    }

    internal record LvhnDocSummary(
        string Name,
        Uri DetailsUri,
        string[] Specialties,
        string[] AreasOfFocus,
        Location[] Locations,
        Uri? ImageUri,
        bool AcceptingNewPatients);

    internal record LvhnDocDetails(
        string BioDescription,
        Experience[] Degrees,
        Experience[] Training,
        Experience[] Certifications,
        Uri? ScholarlyWorksLink,
        string[] ConditionsTreated,
        string[] ServicesOffered
    );

    internal record Experience(string ExperienceType, string Institution, int? Year = null);

    internal record LvhnProfile(LvhnDocSummary? Summary, LvhnDocDetails? Details, string? Error);
}

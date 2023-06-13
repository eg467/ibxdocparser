using HtmlAgilityPack;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ibxdocparser
{
    internal partial class LvhnOrgParser
    {
        public LvhnOrgParser() { }


        private static async Task<string> DownloadHtmlAsync(Uri url)
        {
            using HttpClient client = new();
            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string html = await response.Content.ReadAsStringAsync();
                return html;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred: {ex.Message}");
                throw;
            }
        }


        [GeneratedRegex("page=\\d*&?", RegexOptions.IgnoreCase, "en-US")]
        private static partial Regex PageNumberRemover();

        private Uri SetPageNumber(Uri source, int pageNum)
        {
            var query = System.Web.HttpUtility.ParseQueryString(source.Query);
            query.Set("page", pageNum.ToString());
            var builder = new UriBuilder(source)
            {
                Query = query.ToString()
            };
            return builder.Uri;
        }


        private async Task ReadDocSummaries(Uri listingUrl)
        {
            for (var page = 0; true; page++)
            {
                Uri pageUri = SetPageNumber(listingUrl, page);
                string html = await DownloadHtmlAsync(pageUri);
                var summaries = ParsePageSummaries(html, pageUri);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="html">The HTML data in a search results page</param>
        /// <param name="source">Used to resolve relative URLs</param>
        /// <returns></returns>
        private LvhnDocSummary[] ParsePageSummaries(string html, Uri source)
        {






            return null;
        }


    }


    internal class LvhnSummaryNodeParser
    {
        private readonly HtmlNode _resultNode;
        private readonly Uri _listingSource;

        public LvhnSummaryNodeParser(HtmlNode resultNode, Uri listingSource)
        {
            _resultNode = resultNode;
            _listingSource = listingSource;
        }

        private Lazy<HtmlNode?> InfoNode => new(() =>
        {
            var n = _resultNode.SelectSingleNodeWithClass("info");
            return n ?? throw new InvalidOperationException("Info node not found.");
        });
        private Lazy<HtmlNode?> HighlightsNode => new(() =>
        {
            var n = _resultNode.SelectSingleNodeWithClass("highlights");
            return n ?? throw new InvalidOperationException("Highlights node not found.");
        });

        Uri? ParseImageUri()
        {
            string? relativeSource = InfoNode.Value.SelectSingleNodeWithClass("headshot", "div")
                ?.SelectSingleNode("//img")
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

        string ParseName() =>
            InfoNode.Value.SelectSingleNodeWithClass("field--name-node-title", "div")?.InnerText ?? "";

        Uri ParseDetailsUri()
        {
            var href = InfoNode.Value.SelectSingleNodeWithClass("field--name-node-title", "div")
                ?.SelectSingleNode("//a")
                .GetAttributeValue("href", "");
            return string.IsNullOrEmpty(href)
                ? throw new InvalidOperationException("The result has no link")
                : new Uri(_listingSource, href);
        }

        int? ParseRatingCount()
        {
            var ratingText = InfoNode.Value.SelectSingleNodeWithClass("ds-ratingcount", "*")?.InnerText ?? "";
            return int.TryParse(ratingText, out var rating) ? rating : null;
        }



    }

    public static class WebAgilityPackExtensions
    {

        private const string ClassSelector = "//{0}[contains(@class, '{1}')]";

        public static HtmlNode? SelectSingleNodeWithClass(this HtmlNode node, string className, string tagName = "*") =>
            node.SelectSingleNode(string.Format(ClassSelector, tagName, className));

        public static HtmlNodeCollection SelectNodesWithClass(this HtmlNode node, string className, string tagName = "*") =>
            node.SelectNodes(string.Format(ClassSelector, tagName, className));
    }

    internal record LvhnDocSummary
    {
        public string Name { get; }
        public string Link { get; }
        public string[] Specialties { get; }
        public string[] AreaOfFocus { get; }
        public double Rating { get; }
        public int RatingCount { get; }
        public Location? Location { get; }
        public Uri ImagePage { get; }
    }
}

using HtmlAgilityPack;

namespace ibxdocparser
{
    internal class LvhnSummaryNodesParser
    {
        private readonly HtmlNode[] _resultNodes;
        private readonly Uri _listingSource;

        /// <summary>
        /// Creates an instance of <see cref="LvhnSummaryNodesParser"/>.
        /// </summary>
        /// <param name="html">The source code html of the page.</param>
        /// <param name="listingSource">The URI used to resolve relative links.</param>
        public LvhnSummaryNodesParser(string html, Uri listingSource)
        {
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            var query = $"//div[{Utilities.XpathAttrContains("result-column")}]//article[{Utilities.XpathAttrContains("node--type-doctor")}]";
            var nodes = doc.DocumentNode.SelectNodes(query);

            _resultNodes = nodes?.ToArray()
                ?? Array.Empty<HtmlNode>();
            _listingSource = listingSource;
        }

        public IEnumerable<LvhnDocSummary> Parse()
        {
            return _resultNodes.Select((n, i) =>
            {
                // Create a new document, since it seems to be the only way to default build in caching for different nodes.
                var summary = new LvhnSummaryNodeParser(n.OuterHtml, _listingSource).Parse();
                return summary;
            });
        }
    }
}

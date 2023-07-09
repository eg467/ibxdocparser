using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Text.Json;

namespace ibxdocparser
{

    internal class FullBrowserResponse
    {

        private const string USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36 Edg/114.0.1823.67";

        public FullBrowserResponse() { }

        private static async Task ConfigureWebviewAsync(WebView2 webview)
        {
            await webview.EnsureCoreWebView2Async(null);
            webview.CoreWebView2.Settings.UserAgent = USER_AGENT;
        }

        private static async Task<string> GetResponseContentAsync(CoreWebView2WebResourceResponseView response)
        {
            using var responseStream = await response.GetContentAsync();
            using var reader = new StreamReader(responseStream);
            var responseString = await reader.ReadToEndAsync();
            return responseString;
        }

        public async Task<(string RequestUri, string response)> GetSubRequestResponseAsync<TResult>(Uri uri, Func<CoreWebView2WebResourceRequest, CoreWebView2WebResourceResponseView, bool> predicate, int? timeout = 1)
        {
            var tcs = new TaskCompletionSource<(string RequestUri, string response)>();
            using var cancellationTokenSource = timeout.HasValue ? new CancellationTokenSource(timeout.Value) : new CancellationTokenSource();
            cancellationTokenSource.Token.Register(() => tcs.SetException(new TimeoutException()));
            using var webview = new WebView2();
            await ConfigureWebviewAsync(webview);
            webview.CoreWebView2.WebResourceResponseReceived += async (object? sender, CoreWebView2WebResourceResponseReceivedEventArgs e) =>
            {
                if (predicate(e.Request, e.Response))
                {
                    var responseString = await GetResponseContentAsync(e.Response);
                    tcs.SetResult((e.Request.Uri, responseString));
                }
            };

            webview.CoreWebView2.Navigate(uri.ToString());
            return await tcs.Task;
        }

        public static Task<IEnumerable<(string RequestUri, string response)>> GetSubRequestResponsesAsync<TResult>(Uri uri, Func<CoreWebView2WebResourceRequest, CoreWebView2WebResourceResponseView, bool> predicate, int timeout = 3000)
        {
            return Task.Run(async () =>
            {
                var tcs = new TaskCompletionSource<IEnumerable<(string RequestUri, string response)>>();
                var responses = new List<(string RequestUri, string response)>();
                using var webview = new WebView2();
                await ConfigureWebviewAsync(webview);
                webview.CoreWebView2.WebResourceResponseReceived += async (object? sender, CoreWebView2WebResourceResponseReceivedEventArgs e) =>
                {
                    if (predicate(e.Request, e.Response))
                    {
                        var responseString = await GetResponseContentAsync(e.Response);
                        responses.Add((e.Request.Uri, responseString));
                    }
                };

                webview.CoreWebView2.Navigate(uri.ToString());
                await Task.Delay(timeout);
                return responses as IEnumerable<(string RequestUri, string response)>;
            });
        }

        public static async Task<string> FullHtmlAsync(string uri, int waitAfterNavigationComplete = 500, int? timeout = 10000)
        {
            var tcs = new TaskCompletionSource<string>();
            using var cancellationTokenSource = timeout.HasValue ? new CancellationTokenSource(timeout.Value) : new CancellationTokenSource();
            cancellationTokenSource.Token.Register(() => tcs.SetException(new TimeoutException()));
            using var webview = new WebView2();
            await ConfigureWebviewAsync(webview);

            webview.NavigationCompleted += async (object? sender, CoreWebView2NavigationCompletedEventArgs e) =>
            {
                await Task.Delay(waitAfterNavigationComplete);
                string htmlSource = await webview.CoreWebView2.ExecuteScriptAsync("{source: document.documentElement.outerHTML}");
                htmlSource = JsonSerializer.Deserialize<string>(htmlSource) ?? "";
                tcs.SetResult(htmlSource);
            };

            webview.CoreWebView2.Navigate(uri);
            return await tcs.Task;
        }

    }

}

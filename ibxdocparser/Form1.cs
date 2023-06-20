using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Diagnostics;
using System.Text.Json;

namespace ibxdocparser
{

    public partial class frmIbxDocParser : Form
    {
        private Queue<string> _profileUriQueue = new();
        private readonly IJsonParser<IbxProfile> _profileParser = new IbxProfileJsonParser();

        private readonly AccessDatabase _database;
        private readonly IProfileSaver<IbxProfile> _ibxSaver;
        private readonly IProfileSaver<LvhnProfile> _lvhnSaver;

        public frmIbxDocParser()
        {
            InitializeComponent();
            _database = new("Data.accdb");
            _ibxSaver = new IbxDatabaseSaver(_database);
            _lvhnSaver = new LvhnDatabaseSaver(_database);
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            await _database.InitializeAsync();
            await InitializeWebView();
            NavigateWebViewToSearchHome();

            //webView.CoreWebView2.OpenDevToolsWindow();
        }

        private async Task InitializeWebView()
        {
            System.Diagnostics.Debug.WriteLine("InitializeWebView (start)");
            await webView.EnsureCoreWebView2Async(null);
            webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
            webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
            webView.CoreWebView2.WebResourceResponseReceived += CoreWebView2_WebResourceResponseReceived;
            webView.CoreWebView2.Settings.UserAgent = AppSettings.Default.BrowserUserAgent;
            System.Diagnostics.Debug.WriteLine("InitializeWebView (end)");
        }

        /// <summary>
        /// Web messages are messages sent from JS on web view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="MissingFieldException"></exception>
        private async void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string json = e.WebMessageAsJson;

            var parsingOptions = new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            JsonElement result = JsonSerializer.Deserialize<JsonElement>(json, parsingOptions);

            switch (result.GetPropertyString("type"))
            {
                case "ProfileLinks":
                    var response = JsonSerializer.Deserialize<WebMessage<string[][]>>(json, parsingOptions);
                    var linksByPage = response is not null ? response.Data : throw new MissingFieldException("No payload found");
                    IEnumerable<string> allLinks = linksByPage
                        .SelectMany(l => l)
                        .Select(StandardizeUrlFromCurrent);
                    _profileUriQueue = new Queue<string>(allLinks);

                    // Initiate the first profile after receiving the list of profile links
                    await AdvanceProfileOrSaveParsedProfilesAsync();
                    break;
            }
        }

        /// <summary>
        /// This intercepts AJAX calls from the web page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CoreWebView2_WebResourceResponseReceived(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2WebResourceResponseReceivedEventArgs e)
        {
            static async Task<string> GetResponseContentAsync(CoreWebView2WebResourceResponseView response)
            {
                using var responseStream = await response.GetContentAsync();
                using var reader = new StreamReader(responseStream);
                var responseString = await reader.ReadToEndAsync();
                return responseString;
            }

            try
            {
                var currentBrowserUri = (sender as WebView2)?.Source?.ToString() ?? "";
                // The JSON of details for an individual doctor from the IBX site.
                if (e.Request.Uri.Contains("service/v2/profile", StringComparison.OrdinalIgnoreCase))
                {
                    string profileJson = await GetResponseContentAsync(e.Response);
                    IbxProfile profile = _profileParser.Parse(profileJson);
                    await _ibxSaver.AddProfileAsync(profile);

                    ProfileProcessed((string)(webView.Tag ?? ""));
                    await AdvanceProfileOrSaveParsedProfilesAsync();
                }
            }
            catch (Exception exc)
            {
                Debug.WriteLine(exc.ToString());
                throw;
            }
        }

        /// <summary>
        /// Checked that the processed URI matches the one being processed and dequeues that.
        /// </summary>
        /// <param name="uriProcessed">The URI that was actually processed</param>
        /// <returns></returns>
        private void ProfileProcessed(string uriProcessed)
        {
            if (!_profileUriQueue.TryDequeue(out var expectedUri) || expectedUri != uriProcessed)
            {
                MessageBox.Show("There was a mismatch in the profile being processed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw new InvalidOperationException("The wrong profile was processed.");
            }
        }

        /// <summary>
        /// Trigger a load of the next doctor profile page so its JSON can be intercepted.
        /// </summary>
        /// <returns></returns>
        private bool AdvanceToNextProfile()
        {

            if (!_profileUriQueue.TryPeek(out var uri))
            {
                return false;
            }

            webView.Tag = uri;
            webView.CoreWebView2.Navigate(uri);
            return true;
        }

        /// <summary>
        /// Either go to the next profile Uri if any exist or save the results of all completed ones.
        /// </summary>
        private async Task AdvanceProfileOrSaveParsedProfilesAsync()
        {
            if (_profileUriQueue.Any())
            {
                AdvanceToNextProfile();
            }
            else
            {
                await _ibxSaver.SaveAsync();
                MessageBox.Show("done");
            }
        }

        /// <summary>
        /// Some links are given relative or just as a fragment (e.g. '#/path/data'). This convert them to an absolute format.
        /// </summary>
        /// <param name="href"></param>
        /// <returns></returns>
        /// <exception cref="UriFormatException"></exception>
        private string StandardizeUrlFromCurrent(string href)
        {
            return Uri.IsWellFormedUriString(href, UriKind.Absolute)
                ? href
                : href.StartsWith("#")
                ? new UriBuilder(webView.Source)
                {
                    Fragment = href
                }.Uri.ToString()
                : Uri.IsWellFormedUriString(href, UriKind.Relative)
                ? new Uri(webView.Source, href).ToString()
                : throw new UriFormatException("Invalid link: " + href);
        }


        private void NavigateWebViewToSearchHome() =>
            NavigateWebView(AppSettings.Default.IbxSearchHome);

        private void btnNavigateHome_Click(object sender, EventArgs e) =>
            NavigateWebViewToSearchHome();

        private async void btnParseListings_Click(object sender, EventArgs e)
        {
            await _ibxSaver.StartSessionAsync("Ibx Profile Search", webView.Source);
            var scriptPath = Path.Combine(Environment.CurrentDirectory, "Javascript", "GetIbxDocProfilesFromListing.js");
            string script = File.ReadAllText(scriptPath);
            await webView.CoreWebView2.ExecuteScriptAsync(script);
        }

        private void NavigateWebView(string uri)
        {
            if (Uri.IsWellFormedUriString(uri, UriKind.Absolute))
            {
                webView.CoreWebView2.Navigate(uri);
            }
            else
            {
                MessageBox.Show("Error navigating.");
            }
        }

        private void btnCopyUrl_Click(object sender, EventArgs e)
        {
            string s = Microsoft.VisualBasic.Interaction.InputBox("Enter the URL to search", "Current URL", DefaultResponse: webView.Source.ToString());
            if (!string.IsNullOrEmpty(s))
            {
                NavigateWebView(s);
            }
        }

        private async void btnTest_Click(object sender, EventArgs e)
        {

            string script = File.ReadAllText(@"Javascript\Test.js");
            await webView.CoreWebView2.ExecuteScriptAsync(script);
        }

        private async void btnParseLvhn_Click(object sender, EventArgs e)
        {
            string url = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter the URL to search from https://www.lvhn.org/doctors",
                "Search Listing Page URL",
                DefaultResponse: "https://www.lvhn.org/doctors?keys=Internal%20Medicine&f%5B0%5D=patient_age%3AUnder%2016&f%5B1%5D=specialty%3A2118");

            if (string.IsNullOrEmpty(url))
            {
                return;
            }

            await _lvhnSaver.StartSessionAsync("LVHN Profile Search", new Uri(url));

            List<LvhnProfile> profiles = await LvhnOrgHtmlParser.ParseFullResultsAsync(new Uri(url));

            for (var i = 0; i < profiles.Count; i++)
            {
                var profile = profiles[i];
                Debug.WriteLine($"Parsing profile {i + 1}/{profiles.Count} ({profile.Summary?.Name})");
                try
                {
                    await _lvhnSaver.AddProfileAsync(profile);
                }
                catch (Exception)
                {
                    Debug.WriteLine($"Failed to save profile for {profile.Summary?.Name}");
                    throw;
                }
            }

            await _lvhnSaver.SaveAsync();
            MessageBox.Show("Done");
        }

        private string? SaveExcelFile(Action<string> save)
        {
            while (true)
            {
                if (DialogResult.Cancel == saveExcelFileDialog.ShowDialog())
                {
                    return null;
                }

                try
                {
                    save(saveExcelFileDialog.FileName);
                    MessageBox.Show($"File saved successfully at: {saveExcelFileDialog.FileName}");
                    return saveExcelFileDialog.FileName;
                }
                catch (Exception ex)
                {
                    if (DialogResult.Cancel == MessageBox.Show(ex.Message, "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error))
                    {
                        return null;
                    }
                }

            };
        }

        private async void btnClearDatabase_Click(object sender, EventArgs e)
        {
            var response = MessageBox.Show(
                "Are you sure you want to delete the database file?",
                "Confirm",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Exclamation);

            if (DialogResult.OK == response)
            {
                await _database.ResetAsync();
                MessageBox.Show("The database was deleted", "Success");
            }
        }
    }

    /// <summary>
    /// Format for messages sent to/from client-side js
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class WebMessage<T>
    {
        public string Type { get; set; }
        public T Data { get; set; }
    }
}



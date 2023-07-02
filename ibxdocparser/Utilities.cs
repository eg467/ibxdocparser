using System.Diagnostics;

namespace ibxdocparser
{
    public static class Utilities
    {

        private static readonly Random _random = new();

        public static Task RequestDelayAsync()
        {
            int minDelay = AppSettings.Default.MinDelay;
            int maxDelay = AppSettings.Default.MaxDelay;

            if (minDelay < 0 || maxDelay < 0 || minDelay > maxDelay)
            {
                throw new ArgumentException("The minimum and maximum delays in settings must be greater than or equal to 0.");
            }

            int randomDelay = _random.Next(minDelay, maxDelay + 1);
            return Task.Delay(randomDelay);
        }


        /// <summary>
        /// Downloads a web page's source code contents.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static async Task<string> DownloadHtmlAsync(Uri url)
        {


            using HttpClient client = new();
            try
            {
                // Wait a little between requests to avoid suspicion.
                Thread.Sleep(50);
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string html = await response.Content.ReadAsStringAsync();
                return html;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An HTML downloading error occurred: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Downloads and saves an image from the web.
        /// </summary>
        /// <param name="imageUrl">The remote image source URI.</param>
        /// <param name="savePath">The path the save the image</param>
        public static async Task<bool> DownloadImageAsync(string imageUrl, string savePath)
        {
            using var client = new HttpClient();
            try
            {
                HttpResponseMessage response = await client.GetAsync(imageUrl);
                response.EnsureSuccessStatusCode();

                using Stream stream = await response.Content.ReadAsStreamAsync();
                using FileStream fileStream = new(savePath, FileMode.Create, FileAccess.Write);
                await stream.CopyToAsync(fileStream);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An image downloading error occurred: {ex.Message} ");
                throw;
            }
        }

        public static string XpathAttrContains(string value, string attribute = "@class")
        {
            return $"contains({attribute}, '{value}')";
        }
    }
}

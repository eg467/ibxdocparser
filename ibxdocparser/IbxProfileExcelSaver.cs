using ClosedXML.Excel;
using System.Diagnostics;
using System.Reflection;

namespace ibxdocparser
{
    internal sealed class IbxProfileExcelSaver : IDisposable
    {
        private readonly XLWorkbook _workbook;
        private readonly string _profileWorksheetName;

        /// <summary>
        /// Modify this to change the order of profile fields added to the worksheet columns
        /// </summary>
        private readonly (string Label, Func<IbxProfile, XLCellValue> ValueSelector)[] _fields
            = new (string Label, Func<IbxProfile, XLCellValue> ValueSelector)[]
        {
            ("Id",  (x) => x.Id.ToString()),
            ("First Name",  (x) => x.FirstName ?? ""),
            ("Last Name",  (x) => x.LastName ?? ""),
            ("Full Name",  (x) => x.FullName ?? ""),
            ("Gender",  (x) => x.Gender ?? ""),
            ("Board Certified",  (x) => x.BoardCertified ?? ""),
            ("Education",  (x) => x.Education ?? ""),
            ("Residency",  (x) => x.Residency ?? ""),
            ("Group Affiliations",  (x) => string.Join("\r\n\r\n", x.GroupAffiliations ?? Array.Empty<string>())),
            ("Hospital Affiliations",  (x) => string.Join("\r\n\r\n", x.HospitalAffiliations)),
            ("Locations",  (x) => string.Join("\r\n\r\n", x.Locations.Select(l => l.ToString()))),
            // This will be replaced later by adding the image to the sheet
            ("Image", (x) => "")
        };

        private IXLWorksheet _profileWorksheet => _workbook.Worksheets.First();

        private readonly string _imageDir;
        public IbxProfileExcelSaver(string worksheetName = "Profiles")
        {

            _imageDir = Path.Combine(Path.GetTempPath(), Assembly.GetExecutingAssembly().GetName().Name ?? "", Path.GetRandomFileName());
            Directory.CreateDirectory(_imageDir);
            Debug.WriteLine("Image path for Excel spreadsheet: " + _imageDir);
            _profileWorksheetName = worksheetName;
            _workbook = new XLWorkbook();
            _workbook.Worksheets.Add(_profileWorksheetName);
            SetHeader();
        }

        /// <summary>
        /// Writes the row headers from <see cref="_fields"/>.
        /// </summary>
        private void SetHeader()
        {
            for (var i = 0; i < _fields.Length; i++)
            {
                _profileWorksheet.Cell(1, i + 1).Value = _fields[i].Label;
                _profileWorksheet.Cell(1, i + 1).Style.Font.SetBold(true);
            }
        }

        /// <summary>
        /// Appends the details for a profile to a new line in the spreadsheet.
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        public async Task WriteProfileAsync(IbxProfile profile)
        {
            var worksheet = _profileWorksheet;
            var newRow = worksheet.LastRowUsed().RowBelow();


            for (var i = 0; i < _fields.Length; i++)
            {
                newRow.Cell(i + 1).Value = _fields[i].ValueSelector(profile);
            }

            // Download the image and add it to the spreadsheet if it exists.
            if (!string.IsNullOrWhiteSpace(profile.ImageUri))
            {

                try
                {
                    var imageUriPath = new Uri(profile.ImageUri).GetLeftPart(UriPartial.Path);
                    string extension = Path.GetExtension(imageUriPath);
                    string imagePath = Path.Combine(_imageDir, $"{Guid.NewGuid()}{extension}");
                    await DownloadImageAsync(profile.ImageUri, imagePath);
                    var imageColIdx = Array.FindIndex(_fields, f => f.Label == "Image") + 1;

                    var image = _profileWorksheet.AddPicture(imagePath)
                        .MoveTo(newRow.Cell(imageColIdx));
                }
                catch (Exception)
                {

                    Debug.WriteLine("Failed to download image: " + profile.ImageUri);
                }

            }
        }

        private static async Task DownloadImageAsync(string imageUrl, string savePath)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(imageUrl);
                    response.EnsureSuccessStatusCode();

                    using Stream stream = await response.Content.ReadAsStreamAsync();
                    using FileStream fileStream = new(savePath, FileMode.Create, FileAccess.Write);
                    await stream.CopyToAsync(fileStream);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }
        }

        public void Save(string path)
        {
            _workbook.SaveAs(path);
        }

        public void Dispose()
        {
            _workbook.Dispose();
            Directory.Delete(_imageDir, true);
        }
    }

}


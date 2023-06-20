using ClosedXML.Excel;
using System.Diagnostics;
using System.Reflection;

namespace ibxdocparser
{
    internal class ExcelProfileSaver<TProfile> : IDisposable
    {
        private readonly XLWorkbook _workbook;


        private string _profileWorksheetName = "Profiles";
        public string ProfileWorksheetName
        {
            get => _profileWorksheetName;
            set
            {
                _profileWorksheetName = string.IsNullOrEmpty(value) ? "Profiles" : value;
                _profileWorksheet.Name = _profileWorksheetName;
            }
        }
        private IXLWorksheet _profileWorksheet => _workbook.Worksheets.First();
        private readonly string _imageDir;
        private (string Label, Func<TProfile, XLCellValue> ValueSelector)[] _fields =
            Array.Empty<(string Label, Func<TProfile, XLCellValue> ValueSelector)>();

        public ExcelProfileSaver()
        {
            _imageDir = CreateImageDirectory();
            _workbook = new XLWorkbook();
            _workbook.Worksheets.Add(ProfileWorksheetName);
        }

        public static string CreateImageDirectory()
        {
            var dir = Path.Combine(Path.GetTempPath(), Assembly.GetExecutingAssembly().GetName().Name ?? "", Path.GetRandomFileName());
            Directory.CreateDirectory(dir);
            Debug.WriteLine("Image path for Excel spreadsheet: " + dir);
            return dir;
        }

        public void SetFields((string Label, Func<TProfile, XLCellValue> ValueSelector)[] fields)
        {
            _fields = fields;
            WriteHeader();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="profile"></param>
        /// <returns>The row of the new profile</returns>
        public int AddProfile(TProfile profile)
        {
            var newRow = _profileWorksheet.LastRowUsed().RowBelow();
            for (var i = 0; i < _fields.Length; i++)
            {
                newRow.Cell(i + 1).Value = _fields[i].ValueSelector(profile);
            }
            return newRow.RowNumber();
        }

        public void Dispose()
        {
            _workbook.Dispose();
            Directory.Delete(_imageDir, true);
        }

        /// <summary>
        /// Writes the row headers from <see cref="_fields"/>.
        /// </summary>
        private void WriteHeader()
        {
            _profileWorksheet.Row(1).Clear();
            for (var i = 0; i < _fields.Length; i++)
            {
                _profileWorksheet.Cell(1, i + 1).Value = _fields[i].Label;
                _profileWorksheet.Cell(1, i + 1).Style.Font.SetBold(true);
            }
        }


        public void Save(string path)
        {
            _workbook.SaveAs(path);
        }

        public int IndexOfLabel(string label) =>
            Array.FindIndex(_fields, f => f.Label == label) + 1;

        public async Task SaveImageToCell(Uri remoteUri, int row, int column)
        {
            IXLCell cell = _profileWorksheet.Cell(row, column);
            var imageUriPath = remoteUri.GetLeftPart(UriPartial.Path) ?? "";
            string extension = Path.GetExtension(imageUriPath);
            string imagePath = Path.Combine(_imageDir, $"{Guid.NewGuid()}{extension}");
            await Utilities.DownloadImageAsync(remoteUri.ToString()!, imagePath);
            _profileWorksheet.AddPicture(imagePath).MoveTo(cell);
        }
    }

}


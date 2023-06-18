using ClosedXML.Excel;
using System.Diagnostics;

namespace ibxdocparser
{
    internal sealed class IbxProfileExcelSaver : IDisposable
    {
        private readonly ExcelProfileSaver<IbxProfile> _saver = new("IBX Profile");

        public IbxProfileExcelSaver()
        {
            // Edit this to change the order and contents of the layout
            var fields = new (string Label, Func<IbxProfile, XLCellValue> ValueSelector)[]
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
            _saver.SetFields(fields);
        }



        /// <summary>
        /// Appends the details for a profile to a new line in the spreadsheet.
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        public async Task WriteProfileAsync(IbxProfile profile)
        {

            int row = _saver.AddProfile(profile);

            // Download the image and add it to the spreadsheet if it exists.
            if (profile.ImageUri is not null)
            {
                try
                {
                    var imageUri = new Uri(profile.ImageUri);
                    var imageCol = _saver.IndexOfLabel("Image");
                    await _saver.SaveImageToCell(imageUri, row, imageCol);
                }
                catch (Exception)
                {
                    Debug.WriteLine($"Failed to download image: {profile.ImageUri}");
                }
            }
        }


        public void Save(string path)
        {
            _saver.Save(path);
        }

        public void Dispose()
        {
            _saver.Dispose();
        }
    }

}


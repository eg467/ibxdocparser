using ClosedXML.Excel;
using System.Diagnostics;

namespace ibxdocparser
{
    internal sealed class IbxProfileExcelSaver : IProfileSaver<IbxProfile>, IDisposable
    {
        private readonly ExcelProfileSaver<IbxProfile> _saver = new()
        {
            ProfileWorksheetName = "IBX Profiles"
        };

        public string FilePath { get; set; } = "IbxProfiles.xlsx";

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
                ("Education",  (x) => string.Join("\r\n", x.Education.Select(e=>e.ToString()))),
                ("Residency",  (x) => string.Join("\r\n", x.Residencies.Select(e=>e.ToString()))),
                ("Group Affiliations",  (x) => string.Join("\r\n", x.GroupAffiliations.Select(e=>e.ToString()))),
                ("Hospital Affiliations",  (x) => string.Join("\r\n", x.HospitalAffiliations.Select(e=>e.ToString()))),
                ("Locations",  (x) => string.Join("\r\n\r\n", x.Locations.Select(l => l.ToString()))),
                // This will be replaced later by adding the image to the sheet
                ("Image", (x) => "")
            };
            _saver.SetFields(fields);
        }



        public void Dispose()
        {
            _saver.Dispose();
        }

        public Task StartSessionAsync(string label, Uri? source)
        {
            _saver.ProfileWorksheetName = label;
            return Task.CompletedTask;
        }


        /// <summary>
        /// Appends the details for a profile to a new line in the spreadsheet.
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        public async Task AddProfileAsync(IbxProfile profile)
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

        public Task SaveAsync()
        {
            if (FilePath.Length == 0)
            {
                throw new InvalidOperationException("File name not set.");
            }
            _saver.Save(FilePath);
            return Task.CompletedTask;
        }
    }

}


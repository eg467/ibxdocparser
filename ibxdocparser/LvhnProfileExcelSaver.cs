using ClosedXML.Excel;
using System.Diagnostics;

namespace ibxdocparser
{
    internal interface IProfileSaver<TProfile>
    {
        public Task StartSessionAsync(string label, Uri? source);
        public Task AddProfileAsync(TProfile profile);
        public Task SaveAsync();
    }

    internal sealed class LvhnProfileExcelSaver : IProfileSaver<LvhnProfile>
    {
        private readonly ExcelProfileSaver<LvhnProfile> _saver = new()
        {
            ProfileWorksheetName = " LVHN Profiles"
        };

        public string FilePath { get; set; } = "LvhnProfiles.xlsx";

        private static string ListString(IEnumerable<string>? items)
            => string.Join(", ", items ?? Enumerable.Empty<string>());


        private static string ExperienceString(Experience? experience) => experience is not null
          ? $"{experience.ExperienceType}: {experience.Institution} ({experience.Year})"
          : "";

        private static string ExperienceString(IEnumerable<Experience>? experiences, int index)
            => experiences is not null
                ? ExperienceString(experiences.Skip(index).Take(1).FirstOrDefault())
                : "";

        private static string ExperienceString(IEnumerable<Experience>? experiences)
            => experiences is not null
                ? string.Join("\r\n", experiences.Select(ExperienceString))
                : "";

        public LvhnProfileExcelSaver()
        {
            // Edit this to change the order and contents of the layout
            var fields = new (string Label, Func<LvhnProfile, XLCellValue> ValueSelector)[]
            {
                ("Image", x => ""), // Overwrite this later
                ("Name",  (x) => x.Summary?.Name ?? ""),
                ("Education 1", (LvhnProfile x) => ExperienceString(x?.Details?.Degrees, 0)),
                ("Education 2", (LvhnProfile x) => ExperienceString(x?.Details?.Degrees, 1)),
                ("Education 3", (LvhnProfile x) => ExperienceString(x?.Details?.Degrees, 2)),
                ("All Education", (LvhnProfile x) => ExperienceString(x?.Details?.Degrees)),
                ("Training 1", (LvhnProfile x) => ExperienceString(x?.Details?.Training, 0)),
                ("Training 2", (LvhnProfile x) => ExperienceString(x?.Details?.Training, 1)),
                ("Training 3", (LvhnProfile x) => ExperienceString(x?.Details?.Training, 2)),
                ("All Training", (LvhnProfile x) => ExperienceString(x?.Details?.Training)),
                ("All Certifications", (LvhnProfile x) => ExperienceString(x?.Details?.Certifications)),
                ("Scholarly Works", (LvhnProfile x) => x?.Details?.ScholarlyWorksLink?.ToString() ?? ""),
                ("Link",  (x) => x.Summary?.DetailsUri.ToString() ?? ""),
                ("Specialties",  (x) => ListString(x.Summary?.Specialties)),
                ("Areas of Focus",  (x) => ListString(x.Summary?.AreasOfFocus)),
                ("Conditions Treated",  (x) => ListString(x.Details?.ConditionsTreated)),
                ("Services",  (x) => ListString(x.Details?.ServicesOffered)),
                ("Accepting New Patients", (x) => x?.Summary?.AcceptingNewPatients switch {
                    true => "YES",
                    false => "NO",
                    null => ""
                }),
                ("Location",  (x) => string.Join("\r\n\r\n", x.Summary?.Locations.Select(l => l.ToString()) ?? Array.Empty<string>()))
            };
            _saver.SetFields(fields);
        }

        /// <summary>
        /// Appends the details for a profile to a new line in the spreadsheet.
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        public async Task AddProfileAsync(LvhnProfile profile)
        {
            int row = _saver.AddProfile(profile);

            // Download the image and add it to the spreadsheet if it exists.
            if (profile.Summary?.ImageUri is not null)
            {
                try
                {
                    var imageCol = _saver.IndexOfLabel("Image");
                    await _saver.SaveImageToCell(profile.Summary?.ImageUri!, row, imageCol);
                }
                catch (Exception)
                {
                    Debug.WriteLine("Failed to download image: " + profile.Summary?.ImageUri.ToString());
                }
            }
        }


        public Task SaveAsync()
        {
            if (string.IsNullOrEmpty(FilePath))
            {
                throw new InvalidOperationException("No file path set.");
            }

            _saver.Save(FilePath);
            return Task.CompletedTask;
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
    }
}


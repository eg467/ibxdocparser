using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Diagnostics;
using DAO = Microsoft.Office.Interop.Access.Dao; // Add a reference to the Microsoft.Office.Interop.Access.Dao assembly


namespace ibxdocparser
{
    internal class AccessDatabase
    {
        public int? CurrentSearchId { get; private set; }
        public string FilePath { get; }
        private readonly string _connectionString;

        public AccessDatabase(string path)
        {
            FilePath = path;
            _connectionString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={path};Persist Security Info=False;"; ;
        }

        public Task ResetAsync()
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
            return InitializeAsync();
        }

        private async Task WithConnectionAsync(Func<OleDbConnection, Task> action)
        {
            var connection = new OleDbConnection(_connectionString);
            try
            {
                await connection.OpenAsync();
                await action(connection);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Database Error: {ex}");
                throw;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }


        private async Task<TResult> WithConnectionToResultAsync<TResult>(Func<OleDbConnection, Task<TResult>> action)
        {
            var connection = new OleDbConnection(_connectionString);
            try
            {
                await connection.OpenAsync();
                return await action(connection);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Database Error: {ex}");
                throw;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        private void CreateDatabase()
        {
            var dbEngine = new DAO.DBEngine();
            DAO.Database database = dbEngine.CreateDatabase(FilePath, DAO.LanguageConstants.dbLangGeneral);
            database.Close();
        }

        public async Task InitializeAsync()
        {
            if (File.Exists(FilePath))
            {
                return;
            }

            CreateDatabase();

            string CreateLinkingTable(string tableName, string profileTable, string profileColumn, string linkedTable, string linkedColumn) =>
                $"CREATE TABLE {tableName} ({profileColumn} LONG, {linkedColumn} LONG, CONSTRAINT PK_{tableName} PRIMARY KEY ({profileColumn}, {linkedColumn}), CONSTRAINT FK_{tableName}{profileColumn} FOREIGN KEY ({profileColumn}) REFERENCES {profileTable}(Id), CONSTRAINT FK_{tableName}{linkedColumn} FOREIGN KEY ({linkedColumn}) REFERENCES {linkedTable}(Id))";
            string CreateIbxLinkingTable(string tableName, string linkedTable) =>
                CreateLinkingTable(tableName, "IbxProfiles", "IbxProfilesId", linkedTable, linkedTable + "Id");
            string CreateLvhnLinkingTable(string tableName, string linkedTable) =>
                CreateLinkingTable(tableName, "LvhnProfiles", "LvhnProfilesId", linkedTable, linkedTable + "Id");

            var commands = new (string Label, string Command)[] {
                // TABLES
                ("Searches","CREATE TABLE Searches (Id COUNTER PRIMARY KEY, Label TEXT, Uri TEXT, CreatedOn DATETIME DEFAULT Date())"),
                ("Locations","CREATE TABLE Locations (Id COUNTER PRIMARY KEY, Name TEXT, Street1 TEXT, Street2 TEXT, City TEXT, State TEXT, Zip TEXT, Phone TEXT)"),
                ("ExperienceInstitutions","CREATE TABLE ExperienceInstitutions (Id COUNTER PRIMARY KEY, Name TEXT)"),
                ("ExperienceTypes","CREATE TABLE ExperienceTypes (Id COUNTER PRIMARY KEY, Name TEXT)"),
                ("ExperienceHistories","CREATE TABLE ExperienceHistories (Id COUNTER PRIMARY KEY, ExperienceTypesId LONG, ExperienceInstitutionsId LONG, YearCompleted INTEGER, CONSTRAINT FK_ExperienceHistoriesExperienceTypesId FOREIGN KEY (ExperienceTypesId) REFERENCES ExperienceTypes(Id) ON UPDATE CASCADE ON DELETE CASCADE, CONSTRAINT FK_ExperienceHistoriesExperienceInstitutionsId FOREIGN KEY (ExperienceInstitutionsId) REFERENCES ExperienceInstitutions(Id) ON UPDATE CASCADE ON DELETE CASCADE)"),
                ("Specialties","CREATE TABLE Specialties (Id COUNTER PRIMARY KEY, Name TEXT)"),
                ("AreasOfFocus","CREATE TABLE AreasOfFocus(Id COUNTER PRIMARY KEY, Name TEXT)"),
                ("ConditionsTreated","CREATE TABLE ConditionsTreated (Id COUNTER PRIMARY KEY, Name TEXT)"),
                ("ServicesOffered","CREATE TABLE ServicesOffered (Id COUNTER PRIMARY KEY, Name TEXT)"),

                // IBX
                ("IbxProfiles", "CREATE TABLE IbxProfiles (Id COUNTER PRIMARY KEY, FirstName TEXT, MiddleName TEXT, LastName TEXT, IbxId TEXT, Gender TEXT, BoardCertification Text, ImageUri TEXT)"),
                ("IbxProfileSearchResults", CreateIbxLinkingTable("IbxProfileSearchResults", "Searches")),
                ("IbxProfileExperience", CreateIbxLinkingTable("IbxProfileExperience", "ExperienceHistories")),
                ("IbxProfileGroupAffiliations", CreateIbxLinkingTable("IbxProfileGroupAffiliations", "Locations")),
                ("IbxProfileHospitalAffiliations", CreateIbxLinkingTable("IbxProfileHospitalAffiliations", "Locations")),
                ("IbxProfileLocations", CreateIbxLinkingTable("IbxProfileLocations", "Locations")),

                // LVHN
                ("LvhnProfiles", "CREATE TABLE LvhnProfiles (Id COUNTER PRIMARY KEY, Name TEXT, DetailsUri TEXT, ImageUri TEXT, AcceptingNewPatients YESNO, Bio TEXT, ScholarlyWorksUri TEXT)"),
                ("LvhnProfileSearchResults", CreateLvhnLinkingTable("LvhnProfileSearchResults", "Searches")),
                ("LvhnProfileExperience", CreateLvhnLinkingTable("LvhnProfileExperience", "ExperienceHistories")),
                ("LvhnProfileAreasOfFocus", CreateLvhnLinkingTable("LvhnProfileAreasOfFocus", "AreasOfFocus")),
                ("LvhnProfileConditionsTreated", CreateLvhnLinkingTable("LvhnProfileConditionsTreated", "ConditionsTreated")),
                ("LvhnProfileLocations", CreateLvhnLinkingTable("LvhnProfileLocations", "Locations")),
                ("LvhnProfileServices", CreateLvhnLinkingTable("LvhnProfileServices", "ServicesOffered"))
            };

            await WithConnectionAsync(async (connection) =>
            {
                foreach (var command in commands)
                {
                    Debug.WriteLine($"Running database command: {command.Label} ({command.Command})");
                    var sqlCommand = new OleDbCommand(command.Command, connection);
                    await sqlCommand.ExecuteNonQueryAsync();
                }
            });
        }

        public async Task StartSearchAsync(string label = "", string uri = "")
        {
            CurrentSearchId = await InsertAsync("Searches", new (string f, object? v)[] {
                ("Label", label),
                ("Uri", uri),
                ("CreatedOn", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"))
            });
        }

        #region Query Helpers

        public async Task<int?> FindFieldIdByAttributeAsync(string tableName, string fieldName, string searchValue)
        {
            return await WithConnectionToResultAsync<int?>(async connection =>
            {
                var command = new OleDbCommand($"SELECT Id FROM {tableName} WHERE UCase({fieldName}) = @SearchValue", connection);
                command.Parameters.AddWithValue("@SearchValue", searchValue.ToUpper());

                DbDataReader reader = await command.ExecuteReaderAsync();
                return reader.Read() ? Convert.ToInt32(reader["Id"]) : null;
            });
        }

        public async Task<int> CreateFieldIfNewAsync(string tableName, string fieldName, string fieldValue)
        {
            return await FindFieldIdByAttributeAsync(tableName, fieldName, fieldValue)
                ?? await InsertAsync(tableName, new[] { (fieldName, (object)fieldValue) });
        }

        /// <summary>
        /// Takes a search command that returns the desired Id in the top row, first column
        /// </summary>
        /// <param name="searchCommand"></param>
        /// <returns></returns>
        public static async Task<int?> SearchForIdAsync(OleDbCommand searchCommand)
        {
            object? existingId = await searchCommand.ExecuteScalarAsync();
            return existingId is not null ? Convert.ToInt32(existingId) : null;
        }

        public async Task<int> InsertAsync(string tableName, params (string FieldName, object? Value)[] values)
        {
            var fieldList = values.Select(v => v.FieldName);
            var insertCommandSql = $"INSERT INTO {tableName} ({string.Join(",", fieldList)}) VALUES (@{string.Join(",@", fieldList)})";

            try
            {
                return await WithConnectionToResultAsync(async connection =>
                {
                    var insertCommand = new OleDbCommand(insertCommandSql, connection);
                    foreach (var (FieldName, Value) in values)
                    {
                        insertCommand.Parameters.AddWithValue($"@{FieldName}", Value);
                    }
                    return await ExecuteInsertCommand(insertCommand);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Database insertion error ({insertCommandSql}): {ex}");
                throw;
            }
        }

        private static async Task<int> ExecuteInsertCommand(OleDbCommand command) =>
            command.ExecuteNonQuery() > 0
                ? (await GetLastIdAsync(command.Connection!)).Value
                : throw new Exception("Database insert failed.");

        private static async Task<int?> GetLastIdAsync(OleDbConnection connection)
        {
            var selectCommand = new OleDbCommand("SELECT @@IDENTITY", connection);
            var selectResponse = await selectCommand.ExecuteScalarAsync();
            var newId = Convert.ToInt32(selectResponse);
            return newId;
        }

        private async Task<int> AddOrGetNamedField(string tableName, string value, string fieldName = "Name")
        {
            return await WithConnectionToResultAsync(async connection =>
            {
                var searchQuery = $"SELECT TOP 1 Id FROM {tableName} WHERE UCase({fieldName}) = @Value";
                var searchCommand = new OleDbCommand(searchQuery, connection);
                searchCommand.Parameters.AddWithValue("@Value", value.Trim().ToUpper());

                return await SearchForIdAsync(searchCommand)
                    ?? await InsertAsync(tableName, (fieldName, value.Trim()));
            });
        }


        private Task<int> AddOrGetInstitutionAsync(string name)
        {
            return string.IsNullOrEmpty(name)
                ? throw new ArgumentException("No institution name provided.", nameof(name))
                : AddOrGetNamedField("ExperienceInstitutions", name);
        }

        private Task<int> AddOrGetExperienceTypeAsync(string name)
        {
            return AddOrGetNamedField("ExperienceTypes", name);
        }


        private async Task<int> AddExperienceAsync(Experience experience)
        {
            int typeId = await AddOrGetExperienceTypeAsync(experience.ExperienceType);
            int institutionId = await AddOrGetInstitutionAsync(experience.Institution);

            return await InsertAsync(
             "ExperienceHistories",
             ("ExperienceTypesId", typeId),
             ("ExperienceInstitutionsId", institutionId),
             ("YearCompleted", experience.Year ?? 0));
        }

        public async Task<(int Id, bool Created)> AddOrGetLocation(Location location)
        {
            return await WithConnectionToResultAsync(async connection =>
            {
                var searchQuery = $"SELECT TOP 1 Id FROM Locations WHERE Name = @Name AND Street1 = @Street1 AND Street2 = @Street2 AND City = @City AND State = @State AND Zip = @Zip";
                var searchCommand = new OleDbCommand(searchQuery, connection);
                searchCommand.Parameters.AddWithValue("@Name", location.Name);
                searchCommand.Parameters.AddWithValue("@Street1", location.Address?.Line1 ?? "");
                searchCommand.Parameters.AddWithValue("@Street2", location.Address?.Line2 ?? "");
                searchCommand.Parameters.AddWithValue("@City", location.Address?.City ?? "");
                searchCommand.Parameters.AddWithValue("@State", location.Address?.State ?? "");
                searchCommand.Parameters.AddWithValue("@Zip", location.Address?.Zip ?? "");

                var existingId = await SearchForIdAsync(searchCommand);
                int profileId = existingId ?? await InsertAsync(
                    "Locations",
                    ("Name", location.Name ?? ""),
                    ("Phone", location.Phone ?? ""),
                    ("Street1", location.Address?.Line1 ?? ""),
                    ("Street2", location.Address?.Line2 ?? ""),
                    ("City", location.Address?.City ?? ""),
                    ("State", location.Address?.State ?? ""),
                    ("Zip", location.Address?.Zip ?? "")
                );
                return (profileId, !existingId.HasValue);
            });

        }
        #endregion

        #region IBX



        public async Task AddIbxProfile(IbxProfile profile)
        {

            await WithConnectionAsync(async connection =>
            {
                // Find an existing doc with the same name
                // TODO: Update data with an update?
                var searchQuery = $"SELECT TOP 1 Id FROM IbxProfiles WHERE IbxId = @IbxId";
                var searchCommand = new OleDbCommand(searchQuery, connection);
                searchCommand.Parameters.AddWithValue("@IbxId", profile.Id.ToString());
                int? profileId = await SearchForIdAsync(searchCommand);
                bool newProfile = !profileId.HasValue;
                profileId ??= await InsertAsync(
                        "IbxProfiles",
                        ("FirstName", profile.FirstName ?? ""),
                        ("MiddleName", profile.MiddleName ?? ""),
                        ("LastName", profile.LastName ?? ""),
                        ("IbxId", profile.Id?.ToString() ?? ""),
                        ("Gender", profile.Gender ?? ""),
                        ("BoardCertification", profile.BoardCertified ?? ""),
                        ("ImageUri", profile.ImageUri ?? "")
                );

                if (CurrentSearchId.HasValue)
                {
                    await InsertAsync(
                        "IbxProfileSearchResults",
                        ("IbxProfilesId", profileId),
                        ("SearchesId", CurrentSearchId.Value));
                }

                if (newProfile)
                {
                    foreach (var education in profile.Education.Concat(profile.Residencies))
                    {
                        await InsertAsync(
                            "IbxProfileExperience",
                            ("IbxProfilesId", profileId.Value),
                            ("ExperienceHistoriesId", await AddExperienceAsync(education)));
                    }

                    foreach (var group in profile.GroupAffiliations)
                    {
                        await InsertAsync(
                            "IbxProfileGroupAffiliations",
                            ("IbxProfilesId", profileId.Value),
                            ("LocationsId", (await AddOrGetLocation(group)).Id));
                    }


                    foreach (var location in profile.Locations)
                    {
                        await InsertAsync(
                            "IbxProfileLocations",
                            ("IbxProfilesId", profileId.Value),
                            ("LocationsId", (await AddOrGetLocation(location)).Id));
                    }

                    foreach (var hospital in profile.HospitalAffiliations)
                    {
                        await InsertAsync(
                            "IbxProfileHospitalAffiliations",
                            ("IbxProfilesId", profileId.Value),
                            ("LocationsId", (await AddOrGetLocation(hospital)).Id));
                    }
                }
            });
        }

        #endregion

        #region LVHN
        public async Task AddLvhnProfile(LvhnProfile profile)
        {
            await WithConnectionAsync(async connection =>
            {
                // Find an existing doc with the same name
                // TODO: Update data with an update?
                var searchQuery = $"SELECT TOP 1 Id FROM LvhnProfiles WHERE DetailsUri = @DetailsUri";
                var searchCommand = new OleDbCommand(searchQuery, connection);
                searchCommand.Parameters.AddWithValue("@DetailsUri", profile.Summary?.DetailsUri?.ToString());
                int? profileId = await SearchForIdAsync(searchCommand);
                bool newProfile = !profileId.HasValue;
                profileId ??= await InsertAsync(
                        "LvhnProfiles",
                        ("Name", profile.Summary?.Name ?? ""),
                        ("DetailsUri", profile.Summary?.DetailsUri?.ToString() ?? ""),
                        ("ImageUri", profile.Summary?.ImageUri?.ToString() ?? ""),
                        ("AcceptingNewPatients", profile.Summary?.AcceptingNewPatients ?? false),
                        ("Bio", profile.Details?.BioDescription ?? ""),
                        ("ScholarlyWorksUri", profile.Details?.ScholarlyWorksLink?.ToString() ?? "")
                );

                if (CurrentSearchId.HasValue)
                {
                    await InsertAsync(
                        "LvhnProfileSearchResults",
                        ("LvhnProfilesId", profileId),
                        ("SearchesId", CurrentSearchId.Value));
                }

                if (newProfile)
                {
                    T[] ConcatArrays<T>(params T[]?[] experiences) where T : class =>
                        experiences.Where(e => e is not null).SelectMany(x => x!).ToArray();

                    var experiences = ConcatArrays(profile.Details?.Degrees, profile.Details?.Certifications, profile.Details?.Training);
                    foreach (var experience in experiences)
                    {
                        await InsertAsync(
                            "LvhnProfileExperience",
                            ("LvhnProfilesId", profileId.Value),
                            ("ExperienceHistoriesId", await AddExperienceAsync(experience)));
                    }

                    foreach (var location in profile.Summary?.Locations ?? Array.Empty<Location>())
                    {
                        await InsertAsync(
                            "LvhnProfileLocations",
                            ("LvhnProfilesId", profileId.Value),
                            ("LocationsId", (await AddOrGetLocation(location)).Id));
                    }

                    async Task InsertStringListAsync(string[]? values, string linkingTableName, string linkedTableName)
                    {
                        if (values is null) return;
                        foreach (var value in values)
                        {
                            await InsertAsync(
                               linkingTableName,
                               ("LvhnProfilesId", profileId.Value),
                               ($"{linkedTableName}Id", await AddOrGetNamedField(linkedTableName, value)));
                        }
                    }

                    await InsertStringListAsync(profile?.Summary?.AreasOfFocus, "LvhnProfileAreasOfFocus", "AreasOfFocus");
                    await InsertStringListAsync(profile?.Details?.ConditionsTreated, "LvhnProfileConditionsTreated", "ConditionsTreated");
                    await InsertStringListAsync(profile?.Details?.ServicesOffered, "LvhnProfileServices", "ServicesOffered");
                }

            });

        }

        #endregion
    }

    internal abstract class DatabaseSaver<TProfile> : IProfileSaver<TProfile>
    {
        protected readonly AccessDatabase _db;
        public DatabaseSaver(AccessDatabase db)
        {
            _db = db;
        }

        public abstract Task AddProfileAsync(TProfile profile);

        public Task SaveAsync() => Task.CompletedTask;

        public Task StartSessionAsync(string label, Uri? source) =>
            _db.StartSearchAsync(label, source?.ToString() ?? "");
    }

    internal class LvhnDatabaseSaver : DatabaseSaver<LvhnProfile>
    {
        public LvhnDatabaseSaver(AccessDatabase db) : base(db)
        {
        }

        public override Task AddProfileAsync(LvhnProfile profile) =>
            _db.AddLvhnProfile(profile);
    }

    internal class IbxDatabaseSaver : DatabaseSaver<IbxProfile>
    {
        public IbxDatabaseSaver(AccessDatabase db) : base(db)
        {
        }

        public override Task AddProfileAsync(IbxProfile profile) =>
            _db.AddIbxProfile(profile);
    }

}

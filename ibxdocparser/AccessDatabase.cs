using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Diagnostics;
using DAO = Microsoft.Office.Interop.Access.Dao; // Add a reference to the Microsoft.Office.Interop.Access.Dao assembly


namespace ibxdocparser
{

    internal record SearchSession(int Id, string Name, string ImageDir);

    internal class AccessDatabase
    {
        public SearchSession? CurrentSession { get; private set; }
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

        private static int _constraintIdx = 1;
        public async Task InitializeAsync()
        {
            if (File.Exists(FilePath))
            {
                return;
            }

            var fullDbPath = Path.GetFullPath(FilePath);


            CreateDatabase();


            string idField = "Id COUNTER PRIMARY KEY";

            string ForeignKeyConstraint(string linkedTableName, string? fieldName = null) =>
                $"CONSTRAINT FK_{linkedTableName}{fieldName ?? linkedTableName + "Id"}{_constraintIdx++} FOREIGN KEY ({fieldName ?? linkedTableName + "Id"}) REFERENCES {linkedTableName}(Id) ON UPDATE CASCADE ON DELETE CASCADE";
            string ForeignKeyWithConstraint(string linkedTableName, string? fieldName = null) =>
                $"{fieldName ?? linkedTableName + "Id"} LONG, {ForeignKeyConstraint(linkedTableName, fieldName)}";
            string CreateLinkingTable(string tableName, string profileTable, string profileColumn, string linkedTable, string linkedColumn) =>
                $"CREATE TABLE {tableName} ({profileColumn} LONG, {linkedColumn} LONG, CONSTRAINT PK_{tableName} PRIMARY KEY ({profileColumn}, {linkedColumn}), {ForeignKeyConstraint(profileTable, profileColumn)}, {ForeignKeyConstraint(linkedTable, linkedColumn)})";
            string CreateIbxLinkingTable(string tableName, string linkedTable) =>
                CreateLinkingTable(tableName, "IbxProfiles", "IbxProfilesId", linkedTable, linkedTable + "Id");
            string CreateLvhnLinkingTable(string tableName, string linkedTable) =>
                CreateLinkingTable(tableName, "LvhnProfiles", "LvhnProfilesId", linkedTable, linkedTable + "Id");
            string CreateTableWithNameField(string tableName) => $"CREATE TABLE {tableName} ({idField}, Name TEXT)";


            var commands = new (string Label, string Command)[] {
                // TABLES
                ("Searches", $"CREATE TABLE Searches ({idField}, Label LONGTEXT, Uri LONGTEXT, ImageDir LONGTEXT, Specialty TEXT, CreatedOn DATETIME DEFAULT Date())"),
                ("Locations", $"CREATE TABLE Locations ({idField}, Name LONGTEXT, Street1 LONGTEXT, Street2 LONGTEXT, City LONGTEXT, State LONGTEXT, Zip LONGTEXT, Phone LONGTEXT)"),
                ("ExperienceInstitutions", CreateTableWithNameField("ExperienceInstitutions")),
                ("ExperienceTypes", CreateTableWithNameField("ExperienceTypes")),
                // Level column refers to ExperienceLevel enum
                ("ExperienceTypes (Add Level)", $"ALTER TABLE ExperienceTypes ADD COLUMN ExperienceLevel INTEGER"),
                ("ExperienceHistories", $"CREATE TABLE ExperienceHistories ({idField}, YearCompleted INTEGER, Details TEXT, {ForeignKeyWithConstraint("ExperienceTypes")}, {ForeignKeyWithConstraint("ExperienceInstitutions")})"),
                ("Specialties", CreateTableWithNameField("Specialties")),
                ("AreasOfFocus", CreateTableWithNameField("AreasOfFocus")),
                ("ConditionsTreated",CreateTableWithNameField("ConditionsTreated")),
                ("ServicesOffered", CreateTableWithNameField("ServicesOffered")),

                ("RatingsSources", CreateTableWithNameField("RatingsSources")),
                ("RatingsCategories", CreateTableWithNameField("RatingsCategories")),
                ("Ratings", $"CREATE TABLE Ratings ({idField}, RatingValue DOUBLE, MaxValue INTEGER, {ForeignKeyWithConstraint("RatingsSources")}, {ForeignKeyWithConstraint("RatingsCategories")})"),
                ("ProviderGroups", CreateTableWithNameField("ProviderGroups")),

                // IBX
                ("IbxProfiles", $"CREATE TABLE IbxProfiles ({idField}, FirstName TEXT, MiddleName TEXT, LastName TEXT, IbxId TEXT, Gender TEXT, BoardCertification TEXT, ImageUri LONGTEXT)"),
                ("IbxProfileSearchResults", CreateIbxLinkingTable("IbxProfileSearchResults", "Searches")),
                ("IbxProfileExperience", CreateIbxLinkingTable("IbxProfileExperience", "ExperienceHistories")),
                ("IbxProfileGroupAffiliations", CreateIbxLinkingTable("IbxProfileGroupAffiliations", "Locations")),
                ("IbxProfileHospitalAffiliations", CreateIbxLinkingTable("IbxProfileHospitalAffiliations", "Locations")),
                ("IbxProfileLocations", CreateIbxLinkingTable("IbxProfileLocations", "Locations")),

                // LVHN
                ("LvhnProfiles", $"CREATE TABLE LvhnProfiles ({idField}, Name LONGTEXT, DetailsUri LONGTEXT, ImageUri LONGTEXT, ImagePath LONGTEXT, AcceptingNewPatients YESNO, Bio LONGTEXT, ScholarlyWorksUri LONGTEXT, UseStatus INTEGER, Status TEXT, Comment LONGTEXT, {ForeignKeyWithConstraint("ProviderGroups")} )"),
                ("LvhnProfileSearchResults", CreateLvhnLinkingTable("LvhnProfileSearchResults", "Searches")),
                ("LvhnProfileRatings", CreateLvhnLinkingTable("LvhnProfileRatings", "Ratings")),
                ("LvhnProfileSpecialties", CreateLvhnLinkingTable("LvhnProfileSpecialties", "Specialties")),
                ("LvhnProfileSpecialties (Add IsPrimary Column)", $"ALTER TABLE LvhnProfileSpecialties ADD COLUMN IsPrimary YESNO"),
                ("LvhnProfileExperience", CreateLvhnLinkingTable("LvhnProfileExperience", "ExperienceHistories")),
                ("LvhnProfileAreasOfFocus", CreateLvhnLinkingTable("LvhnProfileAreasOfFocus", "AreasOfFocus")),
                ("LvhnProfileAreasOfFocus (Add IsPrimary Column)", $"ALTER TABLE LvhnProfileAreasOfFocus ADD COLUMN IsPrimary YESNO"),
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

                // Seed Data
                await BuildCommandAsync("ProviderGroups", builder =>
                {
                    var providerGroups = new string[] { "Hospital", "CRNP", "Doctor" };
                    foreach (var providerGroup in providerGroups)
                    {
                        builder.Insert(r => r.AddField("Name", providerGroup));
                    }
                    return builder;
                });

                await BuildCommandAsync("ExperienceTypes", builder =>
                {
                    foreach (var experienceDescription in Enum.GetNames(typeof(ExperienceLevel)))
                    {
                        var experienceLevel = Enum.Parse(typeof(ExperienceLevel), experienceDescription);
                        builder
                            .Insert(r =>
                                r.AddField("Name", experienceDescription)
                                .AddField("ExperienceLevel", experienceLevel));
                    }
                    return builder;
                });

            });
        }


        public async Task StartSearchAsync(string label = "", string uri = "", string specialty = "")
        {
            var fullDbPath = Path.GetFullPath(FilePath);
            var dbDir = Path.GetDirectoryName(fullDbPath) ?? "";
            var imageDir = Path.Combine(
                dbDir,
                "Images",
                DateTime.Now.ToString("s").Replace(':', '_'));

            Directory.CreateDirectory(imageDir);

            int currentSessionId = await InsertAsync("Searches", new (string f, object? v)[] {
                ("Label", label),
                ("Uri", uri),
                ("ImageDir", imageDir),
                ("Specialty", specialty),
                ("CreatedOn", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"))
            });

            CurrentSession = new(currentSessionId, label, imageDir);
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
                ?? await InsertAsync(tableName, new (string FieldName, object? Value)[] { (fieldName, (object)fieldValue) });
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

            IReadOnlyList<int?> results = await BuildCommandWithResultsAsync(tableName, b => b.Insert(f =>
            {
                foreach ((string Name, object? Value) in values)
                {
                    f.AddField(Name, Value);
                }

                return f;
            }));

            return results[0] ?? throw new Exception("Record not created.");
        }

        public Task CommandBuilderHelperAsync(string tableName, Func<AccessCommandBatchBuilder, OleDbConnection, Task> onExecute, Func<AccessCommandBatchBuilder, AccessCommandBatchBuilder> builderConfiguration, OleDbConnection? connection = null)
        {
            try
            {
                Func<Func<OleDbConnection, Task>, Task> WithAppropriateConnection =
                    connection is null
                        ? WithConnectionAsync
                        : (Func<OleDbConnection, Task> action) => action(connection);

                var builder = new AccessCommandBatchBuilder(tableName);
                builder = builderConfiguration(builder);
                return WithAppropriateConnection(connection => onExecute(builder, connection));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Database batch insertion error: {ex}");
                throw;
            }
        }

        public Task BuildCommandAsync(string tableName, Func<AccessCommandBatchBuilder, AccessCommandBatchBuilder> builderConfiguration, OleDbConnection? connection = null) =>
            CommandBuilderHelperAsync(tableName, (b, c) => b.ExecuteAsync(c), builderConfiguration, connection);

        public async Task<IReadOnlyList<int?>> BuildCommandWithResultsAsync(string tableName, Func<AccessCommandBatchBuilder, AccessCommandBatchBuilder> builderConfiguration, OleDbConnection? connection = null)
        {
            IReadOnlyList<int?> results = new List<int?>(0);
            await CommandBuilderHelperAsync(tableName, async (b, c) =>
            {
                results = await b.ExecuteWithResultingIdsAsync(c);
            }, builderConfiguration, connection);

            return results;
        }

        //private static async Task<int> ExecuteInsertCommand(OleDbCommand command) =>
        //    await command.ExecuteNonQueryAsync() > 0
        //        ? (await GetLastIdAsync(command.Connection!)).Value
        //        : throw new Exception("Database insert failed.");

        //private static async Task<int?> GetLastIdAsync(OleDbConnection connection)
        //{
        //    var selectCommand = new OleDbCommand("SELECT @@IDENTITY", connection);
        //    var selectResponse = await selectCommand.ExecuteScalarAsync();
        //    var newId = Convert.ToInt32(selectResponse);
        //    return newId;
        //}

        private async Task<(int Id, bool Created)> AddOrGetNamedField(string tableName, string value, string fieldName = "Name")
        {
            return await WithConnectionToResultAsync(async connection =>
            {
                int? id = await GetIdByFieldValueAsync(tableName, value, connection, fieldName);
                return id.HasValue
                    ? (id.Value, false)
                    : (await InsertAsync(tableName, (fieldName, value.Trim())), true);
            });
        }

        private static Task<int?> GetIdByFieldValueAsync(string tableName, string value, OleDbConnection connection, string fieldName = "Name")
        {
            var searchQuery = $"SELECT TOP 1 Id FROM {tableName} WHERE UCase({fieldName}) = @Value";
            var searchCommand = new OleDbCommand(searchQuery, connection);
            searchCommand.Parameters.AddWithValue("@Value", value.Trim().ToUpper());
            return SearchForIdAsync(searchCommand);
        }

        private async Task<int> AddOrGetInstitutionAsync(string name)
        {
            return string.IsNullOrEmpty(name)
                ? throw new ArgumentException("No institution name provided.", nameof(name))
                : (await AddOrGetNamedField("ExperienceInstitutions", name)).Id;
        }

        private Task<int> AddOrGetExperienceTypeAsync(string name, ExperienceLevel level)
        {
            return WithConnectionToResultAsync(async connection =>
            {
                int? id = await GetIdByFieldValueAsync("ExperienceTypes", name, connection);
                if (id.HasValue)
                {
                    return id.Value;
                }

                var ids = await BuildCommandWithResultsAsync("ExperienceTypes", b =>
                        b.Insert(b2 => b2.AddField("Name", name).AddField("ExperienceLevel", (int)level)));
                return ids[0].HasValue && ids.Count == 1 ? ids[0]!.Value : throw new Exception("Error recording experience.");
            });
        }

        private async Task<int> AddExperienceAsync(Experience experience)
        {
            int typeId = await AddOrGetExperienceTypeAsync(experience.ExperienceType, experience.Level);
            int institutionId = await AddOrGetInstitutionAsync(experience.Institution);

            return await InsertAsync(
             "ExperienceHistories",
             ("ExperienceTypesId", typeId),
             ("Details", experience.Details),
             ("ExperienceInstitutionsId", institutionId),
             ("YearCompleted", experience.Year ?? 0));
        }

        public async Task<(int Id, bool Created)> AddOrGetLocation(Location location)
        {
            return await WithConnectionToResultAsync(async connection =>
            {
                var searchQuery = $"SELECT TOP 1 Id FROM Locations WHERE Name = @Name AND Street1 = @Street1 AND Street2 = @Street2 AND City = @City AND State = @State AND Zip = @Zip";
                var searchCommand = new OleDbCommand(searchQuery, connection);
                searchCommand.Parameters.AddWithValue("@Name", location.Name ?? "");
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

                if (CurrentSession is not null)
                {
                    await InsertAsync(
                        "IbxProfileSearchResults",
                        ("IbxProfilesId", profileId),
                        ("SearchesId", CurrentSession.Id));
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
                string? detailsUri = profile.Summary?.DetailsUri?.ToString();
                int? profileId = detailsUri is not null
                    ? await GetIdByFieldValueAsync("LvhnProfiles", detailsUri, connection, "DetailsUri")
                    : null;

                bool newProfile = !profileId.HasValue;

                // Download Image and save path
                var imagePath = "";
                string? imageUri = profile.Summary?.ImageUri?.ToString();
                if (CurrentSession is not null && imageUri is not null)
                {
                    var filenameWithoutExtension = Path.GetFileNameWithoutExtension(imageUri);
                    // Remove extraneous details after extension, like a query string.
                    var extension = System.Text.RegularExpressions.Regex.Match(
                        Path.GetExtension(imageUri),
                        @"^[-\.\w]+"
                    ).Value;
                    var newFilename = $"{filenameWithoutExtension}-{Guid.NewGuid()}{extension}";
                    imagePath = Path.Combine(CurrentSession.ImageDir, newFilename);
                    await Utilities.DownloadImageAsync(imageUri, imagePath);
                }

                profileId ??= await InsertAsync(
                        "LvhnProfiles",
                        ("Name", profile.Summary?.Name ?? ""),
                        ("DetailsUri", profile.Summary?.DetailsUri?.ToString() ?? ""),
                        ("ImageUri", profile.Summary?.ImageUri?.ToString() ?? ""),
                        ("ImagePath", imagePath),
                        ("AcceptingNewPatients", profile.Summary?.AcceptingNewPatients ?? false),
                        ("Bio", profile.Details?.BioDescription ?? ""),
                        ("ScholarlyWorksUri", profile.Details?.ScholarlyWorksLink?.ToString() ?? "")
                );

                if (CurrentSession is not null)
                {
                    await InsertAsync(
                        "LvhnProfileSearchResults",
                        ("LvhnProfilesId", profileId),
                        ("SearchesId", CurrentSession.Id));
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
                            var linkedRecord = await AddOrGetNamedField(linkedTableName, value);
                            await InsertAsync(
                               linkingTableName,
                               ("LvhnProfilesId", profileId.Value),
                               ($"{linkedTableName}Id", linkedRecord.Id));
                        }
                    }

                    await InsertStringListAsync(profile?.Summary?.AreasOfFocus, "LvhnProfileAreasOfFocus", "AreasOfFocus");
                    await InsertStringListAsync(profile?.Summary?.Specialties, "LvhnProfileSpecialties", "Specialties");
                    await InsertStringListAsync(profile?.Details?.ConditionsTreated, "LvhnProfileConditionsTreated", "ConditionsTreated");
                    await InsertStringListAsync(profile?.Details?.ServicesOffered, "LvhnProfileServices", "ServicesOffered");
                }

            });

        }

        #endregion


        public interface ICommandBuilder
        {
            public Task ExecuteAsync(OleDbConnection connection);
        }

        public interface ICommandBuilderWithIdResults
        {
            public Task<int?> ExecuteWithResultAsync(OleDbConnection connection);
        }

        public class OleDbCommandBuilder : ICommandBuilder
        {
            private readonly string _sql;
            private readonly Action<OleDbCommand> _configureCommand;

            public OleDbCommandBuilder(string sql, Action<OleDbCommand> configureCommand)
            {
                _sql = sql;
                _configureCommand = configureCommand;
            }

            public async Task ExecuteAsync(OleDbConnection connection)
            {
                try
                {
                    var command = new OleDbCommand(_sql, connection);
                    _configureCommand(command);
                    await command.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Database error for query ({_sql}): {ex}");
                    throw;
                }
            }

        }

        public class UpdateCommandBuilder : ICommandBuilder
        {
            private readonly List<(string Name, object? Value)> _fieldValues = new();
            private readonly List<(string Name, object? Value)> _whereFields = new();
            private readonly string _tableName;

            public UpdateCommandBuilder WhereEquals(string field, object? value)
            {
                _whereFields.Add((field, value));
                return this;
            }

            public UpdateCommandBuilder SetField(string name, object? value)
            {
                _fieldValues.Add((name, value));
                return this;
            }

            public UpdateCommandBuilder(string tableName)
            {
                _tableName = tableName;
            }

            public async Task ExecuteAsync(OleDbConnection connection)
            {
                if (_fieldValues.Count == 0)
                {
                    return;
                }

                var assignmentClause = string.Join(",", _fieldValues.Select(f => $"{f.Name} = @{f.Name}"));
                var whereClause = _whereFields.Count > 0 ? string.Join(" AND ", _whereFields.Select(w => $"{w.Name} = @WHERE_{w.Name}")) : " 1 = 1 ";
                var updateCommandSql = $"UPDATE {_tableName} SET {assignmentClause} WHERE {whereClause}";

                try
                {
                    var updateCommand = new OleDbCommand(updateCommandSql, connection);
                    foreach (var (FieldName, Value) in _fieldValues)
                    {
                        updateCommand.Parameters.AddWithValue($"@{FieldName}", Value);
                    }

                    foreach (var (FieldName, Value) in _whereFields)
                    {
                        updateCommand.Parameters.AddWithValue($"@WHERE_{FieldName}", Value);
                    }

                    await updateCommand.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Database update error ({updateCommandSql}): {ex}");
                    throw;
                }
            }
        }

        public class InsertCommandBuilder : ICommandBuilder, ICommandBuilderWithIdResults
        {
            private readonly List<(string Name, object? Value)> _fieldValues = new();
            private readonly string _tableName;

            public InsertCommandBuilder AddField(string name, object? value)
            {
                _fieldValues.Add((name, value));
                return this;
            }

            public InsertCommandBuilder(string tableName)
            {
                _tableName = tableName;
            }

            public Task ExecuteAsync(OleDbConnection connection) => ExecuteAsync(connection, false);
            public Task<int?> ExecuteWithResultAsync(OleDbConnection connection) => ExecuteAsync(connection, true);

            private async Task<int?> ExecuteAsync(OleDbConnection connection, bool queryResultingId)
            {
                var fieldNames = _fieldValues.Select(f => f.Name);
                var insertCommandSql = $"INSERT INTO {_tableName} ({string.Join(",", fieldNames)}) VALUES (@{string.Join(",@", fieldNames)})";

                try
                {
                    var insertCommand = new OleDbCommand(insertCommandSql, connection);
                    foreach (var (FieldName, Value) in _fieldValues)
                    {
                        insertCommand.Parameters.AddWithValue($"@{FieldName}", Value);
                    }
                    await insertCommand.ExecuteNonQueryAsync();
                    return queryResultingId ? await GetLastIdAsync(connection) : -1;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Database insertion error ({insertCommandSql}): {ex}");
                    throw;
                }
            }

            private static async Task<int?> GetLastIdAsync(OleDbConnection connection)
            {
                var selectCommand = new OleDbCommand("SELECT @@IDENTITY", connection);
                var selectResponse = await selectCommand.ExecuteScalarAsync();
                var newId = Convert.ToInt32(selectResponse);
                return newId;
            }

        }

        public class AccessCommandBatchBuilder
        {
            private readonly string _tableName;
            private readonly List<ICommandBuilder> _builders = new();
            private readonly List<ICommandBuilderWithIdResults> _buildersWithResults = new();


            public AccessCommandBatchBuilder(string tableName)
            {
                _tableName = tableName;
            }

            public AccessCommandBatchBuilder FromSqlCommand(string sql, Action<OleDbCommand> action)
            {
                var builder = new OleDbCommandBuilder(sql, action);
                _builders.Add(builder);
                return this;
            }


            public AccessCommandBatchBuilder Insert(Func<InsertCommandBuilder, InsertCommandBuilder> buildFunction)
            {
                var configuredBuilder = buildFunction(new InsertCommandBuilder(_tableName));
                _builders.Add(configuredBuilder);
                _buildersWithResults.Add(configuredBuilder);
                return this;
            }

            public AccessCommandBatchBuilder Update(Func<UpdateCommandBuilder, UpdateCommandBuilder> buildFunction)
            {
                var configuredBuilder = buildFunction(new UpdateCommandBuilder(_tableName));
                _builders.Add(configuredBuilder);
                return this;
            }

            public async Task ExecuteAsync(OleDbConnection connection)
            {
                foreach (var builder in _builders)
                {
                    await builder.ExecuteAsync(connection);
                }
            }

            public async Task<IReadOnlyList<int?>> ExecuteWithResultingIdsAsync(OleDbConnection connection)
            {
                List<int?> results = new();
                foreach (var builder in _buildersWithResults)
                {
                    var id = await builder.ExecuteWithResultAsync(connection);
                    results.Add(id);
                }
                return results;
            }
        }


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

        public Task StartSessionAsync(string label, Uri? source, Dictionary<string, string>? searchTerms = null)
        {
            string specialty = searchTerms?.GetValueOrDefault("Specialty", "") ?? "";
            return _db.StartSearchAsync(label, source?.ToString() ?? "", specialty);
        }

    }

    public record Rating(double Value, int MaxValue, string Category, RatingSource Source, int NumRatings);

    public enum RatingSource
    {
        Lvhn, Ibx, HealthGrades, Vitals
    }

    public enum ExperienceLevel
    {
        Unknown,
        Associates,
        Undergraduate,
        PostBaccalaureate,
        Postgraduate,
        Masters,
        MedicalTraining,
        Internship,
        Residency,
        Fellowship
    }

    public enum UseStatus
    {
        Unknown = 0,
        PrimaryChoice = 1,
        SecondaryChoice = 2,
        Maybe = 5,
        LastChoice = 8,
        NoChance = 9,
        ReviewNeeded = 10
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

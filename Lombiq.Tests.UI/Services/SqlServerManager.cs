using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Lombiq.Tests.UI.Services
{
    public class SqlServerConfiguration
    {
        public const string DatabaseIdPlaceholder = "{{id}}";

        /// <summary>
        /// Gets or sets the template to use to generate SQL Server connection strings. It needs to contain the <see
        /// cref="DatabaseIdPlaceholder"/> placeholder in the database name so unique database names can be generated
        /// for concurrently running UI tests.
        /// </summary>
        public string ConnectionStringTemplate { get; set; } = TestConfigurationManager.GetConfiguration(
            "SqlServerDatabaseConfiguration.ConnectionStringTemplate",
            $"Server=.;Database=LombiqUITestingToolbox_{DatabaseIdPlaceholder};Integrated Security=True;MultipleActiveResultSets=True;");
    }


    public class SqlServerRunningContext
    {
        public string ConnectionString { get; }


        public SqlServerRunningContext(string connectionString) => ConnectionString = connectionString;
    }


    public sealed class SqlServerManager : IDisposable
    {
        private const string DbSnasphotName = "Database.bak";

        private static readonly PortLeaseManager _portLeaseManager;

        private readonly SqlServerConfiguration _configuration;
        private int _databaseId;
        private string _serverName;
        private string _databaseName;


        [SuppressMessage(
            "Minor Code Smell",
            "S3963:\"static\" fields should be initialized inline",
            Justification = "No GetAgentIndexOrDefault() duplication this way.")]
        static SqlServerManager()
        {
            var agentIndexTimesHundred = TestConfigurationManager.GetAgentIndexOrDefault() * 100;
            _portLeaseManager = new PortLeaseManager(13000 + agentIndexTimesHundred, 13099 + agentIndexTimesHundred);
        }

        public SqlServerManager(SqlServerConfiguration configuration) => _configuration = configuration;


        public SqlServerRunningContext CreateDatabase()
        {
            _databaseId = _portLeaseManager.LeaseAvailableRandomPort();

            var connectionString = _configuration.ConnectionStringTemplate
                .Replace(SqlServerConfiguration.DatabaseIdPlaceholder, _databaseId.ToString());

            using var connection = new SqlConnection(connectionString);
            _serverName = connection.DataSource;
            _databaseName = connection.Database;

            var server = CreateServer();

            DropDatabaseIfExists(server);

            // For some reason the database is not always properly created the first time so we need to retry.
            const int maxCount = 5;
            var i = 0;
            while (i < maxCount && !server.Databases.Contains(_databaseName))
            {
                new Database(server, _databaseName).Create();
                i++;
            }

            if (!server.Databases.Contains(_databaseName))
            {
                throw new IOException($"The database {_databaseName} couldn't be created even after {maxCount} tries.");
            }

            return new SqlServerRunningContext(connectionString);
        }

        public void TakeSnapshot(string snapshotDirectoryPath, bool useCompressionIfAvailable = false)
        {
            var filePath = GetSnapshotFilePath(snapshotDirectoryPath);

            if (File.Exists(filePath)) File.Delete(filePath);

            var server = CreateServer();

            KillDatabaseProcesses(server);

            var useCompression = useCompressionIfAvailable &&
                (server.EngineEdition == Edition.EnterpriseOrDeveloper || server.EngineEdition == Edition.Standard);

            var backup = new Backup
            {
                Action = BackupActionType.Database,
                CopyOnly = true,
                Checksum = true,
                Incremental = false,
                ContinueAfterError = false,
                // We don't need compression for setup snapshots as those backups will be only short-lived and we want
                // them to be fast.
                CompressionOption = useCompression ? BackupCompressionOptions.On : BackupCompressionOptions.Off,
                SkipTapeHeader = true,
                UnloadTapeAfter = false,
                NoRewind = true,
                FormatMedia = true,
                Initialize = true,
                Database = _databaseName
            };

            var destination = new BackupDeviceItem(filePath, DeviceType.File);
            backup.Devices.Add(destination);
            // We could use SqlBackupAsync() too but that's not Task-based async, we'd need to subscribe to an event
            // which is messy.
            backup.SqlBackup(server);
        }

        public void RestoreSnapshot(string snapshotDirectoryPath)
        {
            var server = CreateServer();

            if (!server.Databases.Contains(_databaseName))
            {
                throw new InvalidOperationException($"The database {_databaseName} doesn't exist. Something may have dropped it.");
            }

            KillDatabaseProcesses(server);

            var restore = new Restore();
            restore.Devices.AddDevice(GetSnapshotFilePath(snapshotDirectoryPath), DeviceType.File);
            restore.Database = _databaseName;
            restore.ReplaceDatabase = true;

            // Since the DB is restored under a different name this relocation magic needs to happen. Taken from:
            // https://stackoverflow.com/a/17547737/220230.
            var dataFileName = restore.ReadFileList(server).Rows[0][0].ToString();
            var dataPhyisicalFileName = server.Databases[_databaseName].FileGroups[0].Files[0].FileName;
            var dataFile = new RelocateFile
            {
                LogicalFileName = dataFileName,
                PhysicalFileName = dataPhyisicalFileName
            };

            var logFileName = restore.ReadFileList(server).Rows[1][0].ToString();
            var logPhysicalFileName = server.Databases[_databaseName].LogFiles[0].FileName;
            var logFile = new RelocateFile
            {
                LogicalFileName = logFileName,
                PhysicalFileName = logPhysicalFileName
            };

            restore.RelocateFiles.Add(dataFile);
            restore.RelocateFiles.Add(logFile);

            // We're not using SqlRestoreAsync() and SqlVerifyAsync() due to the same reason we're not using
            // SqlBackupAsync().
            restore.SqlRestore(server);
        }

        public void Dispose()
        {
            DropDatabaseIfExists(CreateServer());

            _portLeaseManager.StopLease(_databaseId);
        }


        // It's easier to use the server name directly instead of the connection string as that also requires the
        // referenced database to exist.
        private Server CreateServer() => new Server(_serverName);

        private void DropDatabaseIfExists(Server server)
        {
            if (!server.Databases.Contains(_databaseName)) return;

            KillDatabaseProcesses(server);
            server.Databases[_databaseName].Drop();
        }

        private void KillDatabaseProcesses(Server server) => server.KillAllProcesses(_databaseName);


        private static string GetSnapshotFilePath(string snapshotDirectoryPath) =>
            Path.Combine(Path.GetFullPath(snapshotDirectoryPath), DbSnasphotName);
    }
}

using CliWrap;
using Lombiq.HelpfulLibraries.Cli;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Dmf;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FailedOperationException = Microsoft.SqlServer.Management.Smo.FailedOperationException;

namespace Lombiq.Tests.UI.Services;

public class SqlServerConfiguration
{
    public const string DatabaseIdPlaceholder = "{{id}}";

    /// <summary>
    /// Gets or sets the template to use to generate SQL Server connection strings. It needs to contain the <see
    /// cref="DatabaseIdPlaceholder"/> placeholder in the database name so unique database names can be generated
    /// for concurrently running UI tests.
    /// </summary>
    public string ConnectionStringTemplate { get; set; } = TestConfigurationManager.GetConfiguration(
        "SqlServerDatabaseConfiguration:ConnectionStringTemplate",
        $"Server=.;Database=LombiqUITestingToolbox_{DatabaseIdPlaceholder};Integrated Security=True;" +
        $"MultipleActiveResultSets=True;Connection Timeout=60;ConnectRetryCount=15;ConnectRetryInterval=5");
}

public class SqlServerRunningContext
{
    public string ConnectionString { get; }

    public SqlServerRunningContext(string connectionString) => ConnectionString = connectionString;
}

public sealed class SqlServerManager : IDisposable
{
    private const string DbSnapshotName = "Database.bak";

    private static readonly PortLeaseManager _portLeaseManager;

    private readonly CliProgram _docker = new("docker");
    private readonly SqlServerConfiguration _configuration;
    private int _databaseId;
    private string _serverName;
    private string _databaseName;
    private string _userId;
    private string _password;
    private bool _isDisposed;

    // Not actually unnecessary.
#pragma warning disable IDE0079 // Remove unnecessary suppression
    [SuppressMessage(
        "Performance",
        "CA1810:Initialize reference type static fields inline",
        Justification = "No GetAgentIndexOrDefault() duplication this way.")]
#pragma warning restore IDE0079 // Remove unnecessary suppression
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
            .Replace(SqlServerConfiguration.DatabaseIdPlaceholder, _databaseId.ToTechnicalString(), StringComparison.Ordinal);

        var connection = new SqlConnectionStringBuilder(connectionString);
        _serverName = connection.DataSource;
        _databaseName = connection.InitialCatalog;
        _userId = connection.UserID;
        _password = connection.Password;

        var server = CreateServer();

        DropDatabaseIfExists(server);

        new Database(server, _databaseName).Create();

        return new SqlServerRunningContext(connectionString);
    }

    /// <summary>
    /// Takes a snapshot of the SQL Server database and saves it to the specified directory. If the SQL Server is
    /// running on the local machine's file system, then <paramref name="snapshotDirectoryPathRemote"/> and
    /// <paramref name="snapshotDirectoryPathLocal"/> should be the same or the latter can be left at the default
    /// <see langword="null"/> value. If the server is on a remote location or within a container, then the
    /// <paramref name="snapshotDirectoryPathRemote"/> directory must be mounted to the
    /// <paramref name="snapshotDirectoryPathLocal"/> location on the local file system. For example via a mounted
    /// volume on Docker or an (S)FTP network location mounted as a drive.
    /// </summary>
    /// <param name="snapshotDirectoryPathRemote">
    /// The location of the save directory on the SQL Server's machine.
    /// </param>
    /// <param name="snapshotDirectoryPathLocal">
    /// The location of the directory where the saved database snapshot can be accessed from the local system. If
    /// <see langword="null"/>, it takes on the value of <paramref name="snapshotDirectoryPathRemote"/>. Useful if
    /// for local server only calls.
    /// </param>
    /// <param name="useCompressionIfAvailable">
    /// If set to <see langword="true"/> and the database engine supports it, then
    /// <see cref="BackupCompressionOptions.On"/> will be used.
    /// </param>
    public async Task TakeSnapshotAsync(
        string snapshotDirectoryPathRemote,
        string snapshotDirectoryPathLocal = null,
        string containerName = null,
        bool useCompressionIfAvailable = false)
    {
        var filePathRemote = GetSnapshotFilePath(snapshotDirectoryPathRemote);
        var filePathLocal = GetSnapshotFilePath(snapshotDirectoryPathLocal ?? snapshotDirectoryPathRemote);
        var directoryPathLocal =
            Path.GetDirectoryName(filePathLocal) ??
            throw new InvalidOperandException($"Failed to get the directory path for local path \"{filePathLocal}\".");

        if (!Directory.Exists(directoryPathLocal)) Directory.CreateDirectory(directoryPathLocal);
        if (File.Exists(filePathLocal)) File.Delete(filePathLocal);

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
            Database = _databaseName,
        };

        var destination = new BackupDeviceItem(filePathRemote, DeviceType.File);
        backup.Devices.Add(destination);
        // We could use SqlBackupAsync() too but that's not Task-based async, we'd need to subscribe to an event
        // which is messy.
        await Task.Run(() => backup.SqlBackup(server));

        if (!string.IsNullOrEmpty(containerName))
        {
            if (File.Exists(filePathLocal)) File.Delete(filePathLocal);

            await Cli.Wrap("docker")
                .WithArguments(new[] { "cp", $"{containerName}:{filePathRemote}", filePathLocal })
                .ExecuteAsync();
        }

        if (!File.Exists(filePathLocal))
        {
            throw filePathLocal == filePathRemote
                ? new InvalidOperationException($"A file wasn't created at \"{filePathLocal}\".")
                : new FileNotFoundException(
                    $"A file was created at \"{filePathRemote}\" but it doesn't appear at \"{filePathLocal}\". " +
                    $"Are the two bound together? If you are using Docker, did you set up the local volume?");
        }
    }

    public async Task RestoreSnapshotAsync(
        string snapshotDirectoryPathRemote,
        string snapshotDirectoryPathLocal,
        string containerName)
    {
        if (_isDisposed)
        {
            throw new InvalidOperationException("This instance was already disposed.");
        }

        var server = CreateServer();

        if (!server.Databases.Contains(_databaseName))
        {
            throw new InvalidOperationException($"The database {_databaseName} doesn't exist. Something may have dropped it.");
        }

        if (!string.IsNullOrEmpty(containerName))
        {
            var remote = GetSnapshotFilePath(snapshotDirectoryPathRemote);
            var local = GetSnapshotFilePath(snapshotDirectoryPathLocal);

            // Clean up leftovers.
            await DockerExecuteAsync("rm", "-f", remote);

            // Copy back snapshot.
            await _docker.CommandAsync(CancellationToken.None, "cp", Path.Combine(local), $"{containerName}:{remote}");

            // Reset ownership.
            await DockerExecuteAsync("bash", "-c", $"chown mssql:root '{remote}'");
        }

        KillDatabaseProcesses(server);

        var restore = new Restore();
        restore.Devices.AddDevice(GetSnapshotFilePath(snapshotDirectoryPathRemote), DeviceType.File);
        restore.Database = _databaseName;
        restore.ReplaceDatabase = true;

        // Since the DB is restored under a different name this relocation magic needs to happen. Taken from:
        // https://stackoverflow.com/a/17547737/220230.
        var dataFile = new RelocateFile
        {
            LogicalFileName = restore.ReadFileList(server).Rows[0][0].ToString(),
            PhysicalFileName = server.Databases[_databaseName].FileGroups[0].Files[0].FileName,
        };

        var logFile = new RelocateFile
        {
            LogicalFileName = restore.ReadFileList(server).Rows[1][0].ToString(),
            PhysicalFileName = server.Databases[_databaseName].LogFiles[0].FileName,
        };

        restore.RelocateFiles.Add(dataFile);
        restore.RelocateFiles.Add(logFile);

        // We're not using SqlRestoreAsync() due to the same reason we're not using
        // SqlBackupAsync().
        await Task.Run(() => restore.SqlRestore(server));
    }

    private Task DockerExecuteAsync(string containerName, params string[] command)
    {
        var arguments = new List<object> { "exec", "-u", 0, containerName };
        arguments.AddRange(command);
        return _docker.CommandAsync(CancellationToken.None, arguments);
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        _isDisposed = true;

        DropDatabaseIfExists(CreateServer());

        _portLeaseManager.StopLease(_databaseId);
    }

    // It's easier to use the server name directly instead of the connection string as that also requires the
    // referenced database to exist.
    private Server CreateServer() =>
        string.IsNullOrWhiteSpace(_password)
            ? new Server(_serverName)
            : new Server(new ServerConnection(_serverName, _userId, _password));

    private void DropDatabaseIfExists(Server server)
    {
        if (!server.Databases.Contains(_databaseName)) return;

        const int maxTryCount = 10;
        var i = 0;
        var dbDropExceptions = new List<Exception>(maxTryCount);
        while (i < maxTryCount)
        {
            i++;

            try
            {
                KillDatabaseProcesses(server);
                server.Databases[_databaseName].Drop();

                return;
            }
            catch (FailedOperationException ex)
            {
                dbDropExceptions.Add(ex);

                if (i == maxTryCount)
                {
                    throw new AggregateException(
                        $"Dropping the database {_databaseName} failed {maxTryCount} times and won't be retried again.",
                        dbDropExceptions);
                }

                Thread.Sleep(10000);
            }
        }
    }

    private void KillDatabaseProcesses(Server server)
    {
        try
        {
            server.KillAllProcesses(_databaseName);
        }
        catch (FailedOperationException)
        {
            // This can cause all kinds of random exceptions that don't actually cause any issues when the server is
            // under load.
        }
    }

    private static string GetSnapshotFilePath(string snapshotDirectoryPath) =>
        snapshotDirectoryPath[0] switch
        {
            // Extract "~" home character when working with Unix path from Unix host.
            '~' => Path.Combine(
                Environment.GetEnvironmentVariable("HOME")!,
                snapshotDirectoryPath[2..],
                DbSnapshotName),
            // Ensure proper Unix path in Windows host.
            '/' => $"{snapshotDirectoryPath.TrimEnd('/')}/{DbSnapshotName}",
            _ => Path.Combine(Path.GetFullPath(snapshotDirectoryPath), DbSnapshotName),
        };
}

using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Diagnostics.CodeAnalysis;

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

            // It's easier to use the server name directly instead of the connection string as that also requires the
            // referenced database to exist.
            var server = new Server(_serverName);

            DropDatabaseIfExists(server, _databaseName);

            new Database(server, _databaseName).Create();

            return new SqlServerRunningContext(connectionString);
        }

        public void Dispose()
        {
            var server = new Server(_serverName);
            DropDatabaseIfExists(server, _databaseName);

            _portLeaseManager.StopLease(_databaseId);
        }


        private static void DropDatabaseIfExists(Server server, string databaseName)
        {
            if (!server.Databases.Contains(databaseName)) return;

            server.KillAllProcesses(databaseName);
            server.Databases[databaseName].Drop();
        }

    }
}

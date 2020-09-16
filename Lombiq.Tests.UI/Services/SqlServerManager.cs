using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

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


    public sealed class SqlServerManager : IAsyncDisposable
    {
        private static readonly PortLeaseManager _portLeaseManager;

        private readonly SqlServerConfiguration _configuration;
        private int _databaseId;


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


        public Task<SqlServerRunningContext> CreateDatabaseAsync()
        {
            _databaseId = _portLeaseManager.LeaseAvailableRandomPort();

            var connectionString = _configuration.ConnectionStringTemplate
                .Replace(SqlServerConfiguration.DatabaseIdPlaceholder, _databaseId.ToString());

            return Task.FromResult(new SqlServerRunningContext(connectionString));
        }

        public ValueTask DisposeAsync()
        {
            _portLeaseManager.StopLease(_databaseId);
            return new ValueTask(Task.CompletedTask);
        }
    }
}

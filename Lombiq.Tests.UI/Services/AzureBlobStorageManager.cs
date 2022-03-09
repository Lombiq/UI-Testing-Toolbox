using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Lombiq.Tests.UI.Helpers;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services
{
    public class AzureBlobStorageConfiguration
    {
        /// <summary>
        /// Gets or sets the Azure Blob Storage connection string. Defaults to local development storage (Storage
        /// Emulator). This configuration will be automatically passed to the tested app.
        /// </summary>
        public string ConnectionString { get; set; } = "UseDevelopmentStorage=true";

        /// <summary>
        /// Gets or sets the Azure Blob Storage container name where all the test apps' files will be stored in
        /// subfolders. Defaults to <c>"LombiqUITestingToolbox"</c>. Ff you want to clean up residual files after an
        /// interrupted test execution you can just delete the container (it'll be created if it doesn't exist). This
        /// configuration will be automatically passed to the tested app.
        /// </summary>
        public string ContainerName { get; set; } = "lombiquitestingtoolbox";
    }

    public class AzureBlobStorageRunningContext
    {
        public string BasePath { get; }

        public AzureBlobStorageRunningContext(string basePath) => BasePath = basePath;
    }

    public sealed class AzureBlobStorageManager : IAsyncDisposable
    {
        private static readonly PortLeaseManager _portLeaseManager;

        private readonly AzureBlobStorageConfiguration _configuration;
        private int _folderId;
        private string _basePath;
        private BlobContainerClient _blobContainer;
        private bool _isDisposed;

        // Not actually unnecessary.
#pragma warning disable IDE0079 // Remove unnecessary suppression
        [SuppressMessage(
            "Performance",
            "CA1810:Initialize reference type static fields inline",
            Justification = "No GetAgentIndexOrDefault() duplication this way.")]
#pragma warning restore IDE0079 // Remove unnecessary suppression
        static AzureBlobStorageManager()
        {
            var agentIndexTimesHundred = TestConfigurationManager.GetAgentIndexOrDefault() * 100;
            _portLeaseManager = new PortLeaseManager(14000 + agentIndexTimesHundred, 14099 + agentIndexTimesHundred);
        }

        public AzureBlobStorageManager(AzureBlobStorageConfiguration configuration) => _configuration = configuration;

        public async Task<AzureBlobStorageRunningContext> SetupBlobStorageAsync()
        {
            _blobContainer = new BlobContainerClient(_configuration.ConnectionString, _configuration.ContainerName);
            await CreateContainerIfNotExistsAsync();

            _folderId = _portLeaseManager.LeaseAvailableRandomPort();
            _basePath = _folderId.ToTechnicalString();

            await DropFolderIfExistsAsync();

            return new AzureBlobStorageRunningContext(_basePath);
        }

        public Task TakeSnapshotAsync(string snapshotDirectoryPath)
        {
            var mediaFolderPath = GetMediaFolderPath(snapshotDirectoryPath);

            DirectoryHelper.CreateDirectoryIfNotExists(mediaFolderPath);

            return IterateThroughBlobsAsync(
                blobClient =>
                {
                    var blobUrl = blobClient.Name[(blobClient.Name.IndexOf('/', StringComparison.OrdinalIgnoreCase) + 1)..];
                    var blobPath = blobUrl.ReplaceOrdinalIgnoreCase("/", Path.DirectorySeparatorChar.ToString());
                    var blobFullPath = Path.Combine(mediaFolderPath, blobPath);
                    DirectoryHelper.CreateDirectoryIfNotExists(Path.GetDirectoryName(blobFullPath));
                    return blobClient.DownloadToAsync(blobFullPath);
                });
        }

        public async Task RestoreSnapshotAsync(string snapshotDirectoryPath)
        {
            var mediaFolderPath = GetMediaFolderPath(snapshotDirectoryPath);
            foreach (var filePath in Directory.EnumerateFiles(mediaFolderPath, "*.*", SearchOption.AllDirectories))
            {
                var relativePath = filePath.ReplaceOrdinalIgnoreCase(mediaFolderPath, string.Empty);
                var relativeBlobUrl = relativePath.ReplaceOrdinalIgnoreCase(Path.DirectorySeparatorChar.ToString(), "/");
                var blobClient = _blobContainer.GetBlobClient(_basePath + relativeBlobUrl);
                await blobClient.UploadAsync(filePath);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_isDisposed) return;

            _isDisposed = true;

            await DropFolderIfExistsAsync();

            _portLeaseManager.StopLease(_folderId);
        }

        private Task CreateContainerIfNotExistsAsync() => _blobContainer.CreateIfNotExistsAsync(PublicAccessType.None);

        private Task DropFolderIfExistsAsync() =>
            IterateThroughBlobsAsync(blobClient => blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots));

        private async Task IterateThroughBlobsAsync(Func<BlobClient, Task> blobProcessorAsync)
        {
            var pages = _blobContainer.GetBlobsAsync(BlobTraits.Metadata, BlobStates.None, _basePath).AsPages();
            await foreach (var page in pages)
            {
                foreach (var blob in page.Values)
                {
                    await blobProcessorAsync(_blobContainer.GetBlobClient(blob.Name));
                }
            }
        }

        private static string GetMediaFolderPath(string snapshotDirectoryPath) =>
            Path.Combine(Path.GetFullPath(snapshotDirectoryPath), "App_Data", "Sites", "Default", "Media");
    }
}

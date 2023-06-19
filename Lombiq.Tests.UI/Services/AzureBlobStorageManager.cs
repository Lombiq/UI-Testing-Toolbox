using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Lombiq.HelpfulLibraries.Common.Utilities;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services;

public class AzureBlobStorageConfiguration
{
    /// <summary>
    /// Gets or sets the Azure Blob Storage connection string. Defaults to local development storage (Storage Emulator).
    /// This configuration will be automatically passed to the tested app.
    /// </summary>
    public string ConnectionString { get; set; } = "UseDevelopmentStorage=true";

    /// <summary>
    /// Gets or sets the Azure Blob Storage container name where all the test apps' files will be stored in subfolders.
    /// Defaults to <c>"LombiqUITestingToolbox"</c>. Ff you want to clean up residual files after an interrupted test
    /// execution you can just delete the container (it'll be created if it doesn't exist). This configuration will be
    /// automatically passed to the tested app.
    /// </summary>
    public string ContainerName { get; set; } = "lombiquitestingtoolbox"; // #spell-check-ignore-line
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

    [SuppressMessage(
        "Performance",
        "CA1810:Initialize reference type static fields inline",
        Justification = "No GetAgentIndexOrDefault() duplication this way.")]
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

        _folderId = await _portLeaseManager.LeaseAvailableRandomPortAsync();
        _basePath = _folderId.ToTechnicalString();

        await DropFolderIfExistsAsync();

        return new AzureBlobStorageRunningContext(_basePath);
    }

    public Task TakeSnapshotAsync(string snapshotDirectoryPath)
    {
        var sitesDirectoryPath = SitesDirectoryPath(snapshotDirectoryPath);
        FileSystemHelper.EnsureDirectoryExists(sitesDirectoryPath);

        return IterateThroughBlobsAsync(
            blobClient =>
            {
                var blobUrl = blobClient.Name[(blobClient.Name.IndexOf('/', StringComparison.OrdinalIgnoreCase) + 1)..];

                var blobDirectoryPath = Path.GetDirectoryName(blobUrl);
                var tenantDirectoryName = blobDirectoryPath.IndexOf(Path.DirectorySeparatorChar) != -1
                    ? blobDirectoryPath[..blobDirectoryPath.IndexOf(Path.DirectorySeparatorChar)]
                    : blobDirectoryPath;
                var tenantMediaDirectoryPath = Path.Combine(sitesDirectoryPath, tenantDirectoryName, "Media");

                var fileSubPath = blobUrl[(tenantDirectoryName.Length + 1)..]
                    .ReplaceOrdinalIgnoreCase("/", Path.DirectorySeparatorChar.ToString());
                var fileFullPath = Path.Combine(tenantMediaDirectoryPath, fileSubPath);

                var fileParentDirectoryPath = Path.GetDirectoryName(fileFullPath);
                FileSystemHelper.EnsureDirectoryExists(fileParentDirectoryPath);

                return blobClient.DownloadToAsync(fileFullPath);
            });
    }

    public async Task RestoreSnapshotAsync(string snapshotDirectoryPath)
    {
        var sitesDirectoryPath = SitesDirectoryPath(snapshotDirectoryPath);
        var tenantDirectoryPaths = Directory.GetDirectories(sitesDirectoryPath);

        foreach (var tenantDirectoryPath in tenantDirectoryPaths)
        {
            var tenantDirectoryName = Path.GetFileName(tenantDirectoryPath);
            var tenantMediaDirectoryPath = Path.Combine(tenantDirectoryPath, "Media");

            if (Directory.Exists(tenantMediaDirectoryPath))
            {
                foreach (var filePath in Directory.EnumerateFiles(tenantMediaDirectoryPath, "*.*", SearchOption.AllDirectories))
                {
                    var relativePath = filePath.ReplaceOrdinalIgnoreCase(tenantMediaDirectoryPath, string.Empty);
                    var blobUrl = _basePath + "/" +
                        tenantDirectoryName +
                        relativePath.ReplaceOrdinalIgnoreCase(Path.DirectorySeparatorChar.ToString(), "/");
                    var blobClient = _blobContainer.GetBlobClient(blobUrl);
                    await blobClient.UploadAsync(filePath);
                }
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;

        _isDisposed = true;

        await DropFolderIfExistsAsync();

        await _portLeaseManager.StopLeaseAsync(_folderId);
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

    private static string SitesDirectoryPath(string snapshotDirectoryPath) =>
        Path.Combine(Path.GetFullPath(snapshotDirectoryPath), "App_Data", "Sites");
}

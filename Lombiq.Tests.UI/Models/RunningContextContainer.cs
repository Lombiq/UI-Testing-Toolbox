using Lombiq.Tests.UI.Services;

namespace Lombiq.Tests.UI.Models;

public record RunningContextContainer(
    SqlServerRunningContext SqlServerRunningContext,
    SmtpServiceRunningContext SmtpServiceRunningContext,
    AzureBlobStorageRunningContext AzureBlobStorageRunningContext,
    ElasticsearchRunningContext ElasticsearchRunningContext);

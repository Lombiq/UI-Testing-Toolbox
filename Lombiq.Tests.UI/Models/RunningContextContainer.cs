using Lombiq.Tests.UI.Services;

namespace Lombiq.Tests.UI.Models;

public class RunningContextContainer(
    SqlServerRunningContext sqlServerContext,
    SmtpServiceRunningContext smtpContext,
    AzureBlobStorageRunningContext blobStorageContext)
{
    public SqlServerRunningContext SqlServerRunningContext { get; } = sqlServerContext;
    public SmtpServiceRunningContext SmtpServiceRunningContext { get; } = smtpContext;
    public AzureBlobStorageRunningContext AzureBlobStorageRunningContext { get; } = blobStorageContext;
}

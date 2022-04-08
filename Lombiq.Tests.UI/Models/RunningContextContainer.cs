using Lombiq.Tests.UI.Services;

namespace Lombiq.Tests.UI.Models;

public class RunningContextContainer
{
    public SqlServerRunningContext SqlServerRunningContext { get; }
    public SmtpServiceRunningContext SmtpServiceRunningContext { get; }
    public AzureBlobStorageRunningContext AzureBlobStorageRunningContext { get; }

    public RunningContextContainer(
        SqlServerRunningContext sqlServerContext,
        SmtpServiceRunningContext smtpContext,
        AzureBlobStorageRunningContext blobStorageContext)
    {
        SqlServerRunningContext = sqlServerContext;
        SmtpServiceRunningContext = smtpContext;
        AzureBlobStorageRunningContext = blobStorageContext;
    }
}

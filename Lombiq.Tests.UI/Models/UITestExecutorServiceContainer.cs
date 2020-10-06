using Lombiq.Tests.UI.Services;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Models
{
    public sealed class UITestExecutorServiceContainer : IAsyncDisposable
    {
        public UITestContext Context { get; set; }
        public SqlServerManager SqlServerManager { get; set; }
        public SqlServerRunningContext SqlServerContext { get; set; }
        public SmtpService SmtpService { get; set; }
        public OrchardCoreInstance ApplicationInstance { get; set; }


        public ValueTask DisposeAsync()
        {
            Context?.Scope?.Dispose();
            SqlServerManager?.Dispose();

            return SmtpService == null && ApplicationInstance == null
                ? default
                : DisposeInnerAsync();
        }

        private async ValueTask DisposeInnerAsync()
        {
            if (SmtpService != null) await SmtpService.DisposeAsync();
            if (ApplicationInstance != null) await ApplicationInstance.DisposeAsync();
        }
    }
}

using Lombiq.Tests.UI.Services;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Models
{
    /// <summary>
    /// A common container for various <see cref="IDisposable"/>/<see cref="IAsyncDisposable"/> services/objects used in
    /// <see cref="UITestExecutor"/>. The purpose is to encapsulate safe disposal with no risk of
    /// <see cref="NullReferenceException"/>.
    /// </summary>
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

            // If both are null, don't needlessly start up a state machine by returning DisposeInnerAsync.
            // Note: default in ValueTask is equivalent to Task.CompletedTask in classic Task.
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

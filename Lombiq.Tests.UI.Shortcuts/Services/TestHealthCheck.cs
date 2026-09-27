#nullable enable

using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Shortcuts.Services;

public class TestHealthCheck : IHealthCheck
{
    private readonly TestHealthCheckStatusAccessor _accessor;

    public TestHealthCheck(TestHealthCheckStatusAccessor accessor) => _accessor = accessor;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new HealthCheckResult(_accessor.Status, _accessor.Description));
}

public class TestHealthCheckStatusAccessor
{
    public HealthStatus Status { get; set; } = HealthStatus.Healthy;
    public string Description { get; set; } = nameof(TestHealthCheck);
}

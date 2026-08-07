using Lombiq.HelpfulLibraries.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services;

/// <summary>
/// Service for acquiring a lease on a given network port number between concurrent processes.
/// </summary>
/// <remarks>
/// <para>You may think it's about managing the rent of a sea harbor but rest assured it isn't.</para>
/// </remarks>
[SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "This is because SemaphoreSlim but it's not actually necessary to dispose in this case: " +
        "https://stackoverflow.com/questions/32033416/do-i-need-to-dispose-a-semaphoreslim. Making this class " +
        "IDisposable would need disposing static members above on app shutdown, which is unreliable.")]
public class PortLeaseManager
{
    private readonly IEnumerable<int> _availablePortsRange;
    private readonly HashSet<int> _usedPorts = [];
    private readonly SemaphoreSlim _portAcquisitionLock = new(1, 1);

    public PortLeaseManager(int lowerBound, int upperBound) =>
        _availablePortsRange = Enumerable.Range(lowerBound, upperBound - lowerBound);

    public async Task<int> LeaseAvailableRandomPortAsync(CancellationToken cancellationToken)
    {
        await _portAcquisitionLock.WaitAsync(cancellationToken);

        int port;

        try
        {
            // Filter out ports already leased by this process AND ports already in use by the OS (e.g., from other
            // processes on the runner). This prevents smtp4dev and similar services from failing to bind because
            // another process has already claimed the port.
            var availablePorts = _availablePortsRange
                .Except(_usedPorts)
                .Where(IsPortFreeOnOs)
                .ToList();

            if (availablePorts.Count == 0)
            {
                throw new InvalidOperationException("No available ports to lease. Check if the range is too small or if you don't release ports.");
            }

            port = availablePorts[new NonSecurityRandomizer().GetFromRange(availablePorts.Count)];
            _usedPorts.Add(port);
        }
        finally
        {
            _portAcquisitionLock.Release();
        }

        return port;
    }

    public async Task StopLeaseAsync(int port, CancellationToken cancellationToken)
    {
        await _portAcquisitionLock.WaitAsync(cancellationToken);

        _usedPorts.Remove(port);

        _portAcquisitionLock.Release();
    }

    private static bool IsPortFreeOnOs(int port)
    {
        try
        {
            using var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }
}

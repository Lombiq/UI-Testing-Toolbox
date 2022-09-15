using Lombiq.HelpfulLibraries.Common.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services;

/// <summary>
/// Service for acquiring a lease on a given network port number between concurrent processes.
/// </summary>
/// <remarks>
/// <para>You may think it's about managing the rent of a sea harbor but rest assured it isn't.</para>
/// </remarks>
public class PortLeaseManager
{
    private readonly IEnumerable<int> _availablePortsRange;
    private readonly HashSet<int> _usedPorts = new();
    private readonly SemaphoreSlim _portAcquisitionLock = new(1, 1);

    public PortLeaseManager(int lowerBound, int upperBound) =>
        _availablePortsRange = Enumerable.Range(lowerBound, upperBound - lowerBound);

    public async Task<int> LeaseAvailableRandomPortAsync()
    {
        await _portAcquisitionLock.WaitAsync();

        int port;

        try
        {
            var availablePorts = _availablePortsRange.Except(_usedPorts).ToList();

            port = availablePorts[new NonSecurityRandomizer().GetFromRange(availablePorts.Count)];
            _usedPorts.Add(port);
        }
        finally
        {
            _portAcquisitionLock.Release();
        }

        return port;
    }

    public async Task StopLeaseAsync(int port)
    {
        await _portAcquisitionLock.WaitAsync();

        _usedPorts.Remove(port);

        _portAcquisitionLock.Release();
    }
}

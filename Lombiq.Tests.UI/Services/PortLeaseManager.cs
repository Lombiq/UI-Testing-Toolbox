using Lombiq.HelpfulLibraries.Common.Utilities;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
[SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "This is because SemaphoreSlim but it's not actually necessary to dispose in this case: " +
        "https://stackoverflow.com/questions/32033416/do-i-need-to-dispose-a-semaphoreslim. Making this class " +
        "IDisposable would need disposing static members above on app shutdown, which is unreliable.")]
public class PortLeaseManager(int lowerBound, int upperBound)
{
    private readonly IEnumerable<int> _availablePortsRange = Enumerable.Range(lowerBound, upperBound - lowerBound);
    private readonly HashSet<int> _usedPorts = [];
    private readonly SemaphoreSlim _portAcquisitionLock = new(1, 1);

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

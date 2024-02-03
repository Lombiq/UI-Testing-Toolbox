using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lombiq.Tests.UI.Services.Counters.Configuration;

public class RunningPhaseCounterConfiguration : PhaseCounterConfiguration,
    IDictionary<ICounterConfigurationKey, CounterConfiguration>
{
    private readonly IDictionary<ICounterConfigurationKey, CounterConfiguration> _configurations =
        new ConcurrentDictionary<ICounterConfigurationKey, CounterConfiguration>();

    public CounterConfiguration this[ICounterConfigurationKey key]
    {
        get => _configurations[key];
        set => _configurations[key] = value;
    }

    public ICollection<ICounterConfigurationKey> Keys => _configurations.Keys;

    public ICollection<CounterConfiguration> Values => _configurations.Values;

    public int Count => _configurations.Count;

    public bool IsReadOnly => false;

    public void Add(ICounterConfigurationKey key, CounterConfiguration value) => _configurations.Add(key, value);
    public void Add(KeyValuePair<ICounterConfigurationKey, CounterConfiguration> item) => _configurations.Add(item);
    public void Clear() => _configurations.Clear();
    public bool Contains(KeyValuePair<ICounterConfigurationKey, CounterConfiguration> item) =>
        _configurations.Contains(item);
    public bool ContainsKey(ICounterConfigurationKey key) => _configurations.ContainsKey(key);
    public void CopyTo(KeyValuePair<ICounterConfigurationKey, CounterConfiguration>[] array, int arrayIndex) =>
        _configurations.CopyTo(array, arrayIndex);
    public IEnumerator<KeyValuePair<ICounterConfigurationKey, CounterConfiguration>> GetEnumerator() =>
        _configurations.GetEnumerator();
    public bool Remove(ICounterConfigurationKey key) => _configurations.Remove(key);
    public bool Remove(KeyValuePair<ICounterConfigurationKey, CounterConfiguration> item) =>
        _configurations.Remove(item);
    public bool TryGetValue(ICounterConfigurationKey key, [MaybeNullWhen(false)] out CounterConfiguration value) =>
        _configurations.TryGetValue(key, out value);
    IEnumerator IEnumerable.GetEnumerator() => _configurations.GetEnumerator();
}

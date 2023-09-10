using System.Collections.Generic;

namespace Lombiq.Tests.UI.Services.Counters;

// The Equals must be implemented in consumer classes.
#pragma warning disable S4035 // Classes implementing "IEquatable<T>" should be sealed
public abstract class CounterKey : ICounterKey
#pragma warning restore S4035 // Classes implementing "IEquatable<T>" should be sealed
{
    public abstract string DisplayName { get; }
    public abstract bool Equals(ICounterKey other);
    protected abstract int HashCode();
    public override bool Equals(object obj) => Equals(obj as ICounterKey);
    public override int GetHashCode() => HashCode();
    public abstract IEnumerable<string> Dump();
}

using System;
using System.Collections.Generic;

namespace Lombiq.Tests.UI.AppExtensions.SqlQueryMonitoring;

/// <summary>
/// Stores SQL query monitoring summaries for the current tenant scope.
/// </summary>
public interface ISqlQueryMonitoringStore
{
    /// <summary>
    /// Adds a completed monitoring summary to the store.
    /// </summary>
    void AddSummary(SqlQueryMonitoringSummary summary);

    /// <summary>
    /// Removes and returns the most recent summary from the store, if any.
    /// </summary>
    bool TryDequeueLatest(out SqlQueryMonitoringSummary summary);

    /// <summary>
    /// Clears all stored summaries.
    /// </summary>
    void Clear();
}

/// <summary>
/// A thread-safe, bounded store of recent SQL query monitoring summaries.
/// </summary>
public sealed class SqlQueryMonitoringStore : ISqlQueryMonitoringStore
{
    private const int MaxEntries = 50;

    private readonly BoundedRingBuffer<SqlQueryMonitoringSummary> _summaries = new(MaxEntries);

    public void AddSummary(SqlQueryMonitoringSummary summary)
    {
        if (summary == null) return;

        _summaries.Add(summary);
    }

    public bool TryDequeueLatest(out SqlQueryMonitoringSummary summary) =>
        _summaries.TryTakeLatest(
            candidate => candidate.Executions.Count != 0,
            out summary);

    public void Clear() => _summaries.Clear();

    private sealed class BoundedRingBuffer<T>
    {
        private readonly T[] _buffer;
        private readonly object _lock = new();
        private int _start;
        private int _count;

        public BoundedRingBuffer(int capacity)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
            _buffer = new T[capacity];
        }

        public void Add(T item)
        {
            lock (_lock)
            {
                var index = (_start + _count) % _buffer.Length;

                if (_count == _buffer.Length)
                {
                    _buffer[_start] = item;
                    _start = (_start + 1) % _buffer.Length;
                }
                else
                {
                    _buffer[index] = item;
                    _count++;
                }
            }
        }

        public bool TryTakeLatest(Predicate<T> predicate, out T item)
        {
            lock (_lock)
            {
                if (_count == 0)
                {
                    item = default;
                    return false;
                }

                var items = GetOrderedItems();
                var index = items.FindLastIndex(candidate => candidate != null && predicate?.Invoke(candidate) != false);
                if (index < 0) index = items.Count - 1;

                item = items[index];
                items.RemoveAt(index);
                Rebuild(items);
                return item != null;
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                Array.Clear(_buffer, 0, _buffer.Length);
                _start = 0;
                _count = 0;
            }
        }

        private List<T> GetOrderedItems()
        {
            var items = new List<T>(_count);
            for (var i = 0; i < _count; i++)
            {
                var index = (_start + i) % _buffer.Length;
                items.Add(_buffer[index]);
            }

            return items;
        }

        private void Rebuild(List<T> items)
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            _start = 0;
            _count = items.Count;

            for (var i = 0; i < items.Count; i++)
            {
                _buffer[i] = items[i];
            }
        }
    }
}

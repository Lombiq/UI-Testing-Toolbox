using System;
using System.Collections.Generic;

namespace Lombiq.Tests.UI.Helpers;

public class GenericComparer<T> : IComparer<T>
{
    private readonly Func<T, T, int> _compare;

    public GenericComparer(Func<T, T, int> compare) => _compare = compare;

    public int Compare(T x, T y) => _compare(x, y);
}

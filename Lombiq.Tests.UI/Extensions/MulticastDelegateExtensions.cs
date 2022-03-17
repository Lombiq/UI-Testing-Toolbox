using System.Linq;

namespace System;

public static class MulticastDelegateExtensions
{
    /// <summary>
    /// Removes all instances of the delegate <paramref name="delegateToRemove"/> from the given <see
    /// cref="MulticastDelegate"/>.
    /// </summary>
    public static T RemoveAll<T>(this T multicastDelegate, T delegateToRemove)
        where T : MulticastDelegate
    {
        if (multicastDelegate == null) return default;

        var handlerName = delegateToRemove.Method.Name;
        return (T)Delegate.RemoveAll(
            multicastDelegate,
            multicastDelegate.GetInvocationList().LastOrDefault(handler => handler.Method.Name == handlerName));
    }
}

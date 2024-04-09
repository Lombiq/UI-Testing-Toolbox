using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;

namespace Shouldly;

public static class ShouldlyExtensions
{
    /// <summary>
    /// Calls <see cref="object.ToString()"/> on both <paramref name="actual"/> and <paramref name="expected"/> (unless
    /// they are <see langword="null"/>) and checks if they are the same.
    /// </summary>
    [SuppressMessage("Code Smell", "S4225:Extension methods should not extend \"object\"", Justification = "This is what Shouldly does.")]
    public static void ShouldBeAsString(this object actual, object expected, string customMessage = null)
    {
        // We need this variable because the null-forgiving operator is shortcutting. This way the ShouldBe is called
        // even if actual is null.
        var actualText = actual?.ToString();

        actualText.ShouldBe(expected?.ToString(), customMessage);
    }

    /// <summary>
    /// Filters the provided <paramref name="enumerable"/> by the <paramref name="condition"/>. If the result is not
    /// empty, a <see cref="ShouldAssertException"/> is thrown. A JSON serialized string of the results is provided as
    /// the custom message of the exception.
    /// </summary>
    /// <remarks><para>
    /// This extension method is similar to <c>enumerable.ShouldNotContain(condition)</c>, but it offers much better
    /// developer experience because all the offending items are listed right there in the exception message. The
    /// overhead is minimal during non-exceptional operation (only the creation of an empty list), and it's well worth
    /// the added clarity during failure.
    /// </para></remarks>
    public static void ShouldBeEmptyWhen<TItem>(
        this IEnumerable<TItem> enumerable,
        Func<TItem, bool> condition,
        JsonSerializerOptions jsonSerializerOptions = null) =>
        enumerable.ShouldBeEmptyWhen<TItem, TItem>(condition, messageTransform: null, jsonSerializerOptions);

    /// <inheritdoc cref="ShouldBeEmptyWhen{TItem}"/>
    /// <param name="messageTransform">
    /// When not <see langword="null"/>, the results are transformed with this method before they
    /// are serialized into JSON.
    /// </param>
    public static void ShouldBeEmptyWhen<TItem, TMessage>(
        this IEnumerable<TItem> enumerable,
        Func<TItem, bool> condition,
        Func<TItem, TMessage> messageTransform,
        JsonSerializerOptions jsonSerializerOptions = null)
    {
        var results = enumerable.Where(condition).ToList();
        if (results.Count == 0) return;

        var message = messageTransform == null
            ? JsonSerializer.Serialize(results, jsonSerializerOptions)
            : JsonSerializer.Serialize(results.Select(messageTransform), jsonSerializerOptions);
        results.ShouldBeEmpty(message); // This will always throw at this point.
    }
}

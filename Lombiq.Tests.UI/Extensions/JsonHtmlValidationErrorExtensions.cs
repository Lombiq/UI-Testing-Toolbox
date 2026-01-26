using Lombiq.Tests.UI.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lombiq.Tests.UI.Models;

public static class JsonHtmlValidationErrorExtensions
{
    /// <summary>
    /// Remove entries from <paramref name="errors"/> if they return <see langword="false"/> when passed into any of the
    /// <paramref name="filters"/>.
    /// </summary>
    internal static IList<JsonHtmlValidationError> RemoveIfFalse(
        this IList<JsonHtmlValidationError> errors,
        IEnumerable<Func<JsonHtmlValidationError, bool>> filters)
    {
        foreach (var filter in filters.Where(filter => filter != null))
        {
            errors.RemoveAll(error => !filter(error));
            if (errors.Count == 0) return errors;
        }

        return errors;
    }

    /// <summary>
    /// Return a new list that filters out items using <see cref="HtmlValidationConfiguration.HtmlValidationFilters"/>.
    /// </summary>
    public static IList<JsonHtmlValidationError> FilterWithConfiguration(
        this IEnumerable<JsonHtmlValidationError> errors,
        HtmlValidationConfiguration configuration) =>
        errors.ToList().RemoveIfFalse(configuration.HtmlValidationFilters.Values);
}

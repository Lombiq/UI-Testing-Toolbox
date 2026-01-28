using System;
using System.Collections.Generic;
using System.Linq;

namespace Lombiq.Tests.UI.Models;

public static class HtmlValidationErrorExtensions
{
    /// <summary>
    /// Remove entries from <paramref name="errors"/> if they return <see langword="false"/> when passed into any of the
    /// <paramref name="filters"/>.
    /// </summary>
    internal static IList<HtmlValidationError> RemoveIfFalse(
        this IList<HtmlValidationError> errors,
        IEnumerable<Func<HtmlValidationError, bool>> filters)
    {
        foreach (var filter in filters.Where(filter => filter != null))
        {
            errors.RemoveAll(error => !filter(error));
            if (errors.Count == 0) return errors;
        }

        return errors;
    }
}

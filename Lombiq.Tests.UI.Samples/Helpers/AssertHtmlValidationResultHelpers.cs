using Atata.HtmlValidation;
using Lombiq.Tests.UI.Extensions;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Samples.Helpers
{
    public static class AssertHtmlValidationResultHelpers
    {
        public static readonly Func<HtmlValidationResult, Task> DefaultAssertHtmlValidationOutput =
            async validationResult =>
            {
                // Placeholder for now.
                var errors = (await validationResult.GetErrorsAsync())
                    .Where(error => true);

                errors.ShouldBeEmpty();
            };
    }
}

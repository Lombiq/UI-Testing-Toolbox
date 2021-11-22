using Atata;

namespace Lombiq.Tests.UI.Components
{
    [ControlDefinition("div[contains(concat(' ', normalize-space(@class), ' '), ' validation-summary-errors ')]/ul/li")]
    public sealed class ValidationSummaryError<TOwner> : Text<TOwner>
        where TOwner : PageObject<TOwner>
    {
        public new FieldVerificationProvider<string, ValidationSummaryError<TOwner>, TOwner> Should => new(this);
    }
}

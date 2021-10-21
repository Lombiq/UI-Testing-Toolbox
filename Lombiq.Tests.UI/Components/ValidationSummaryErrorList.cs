using Atata;

namespace Lombiq.Tests.UI.Components
{
    public sealed class ValidationSummaryErrorList<TOwner> : ControlList<ValidationSummaryError<TOwner>, TOwner>
        where TOwner : PageObject<TOwner>
    {
    }
}

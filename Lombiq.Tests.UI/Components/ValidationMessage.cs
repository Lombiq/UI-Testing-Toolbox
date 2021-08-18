using Atata;

namespace Lombiq.Tests.UI.Components
{
    [ControlDefinition(
        "div[contains(concat(' ', normalize-space(@class), ' '), ' form-group ')]" +
        "//span[contains(concat(' ', normalize-space(@class), ' '), ' field-validation-error ')]")]
    public class ValidationMessage<TOwner> : Text<TOwner>
        where TOwner : PageObject<TOwner>
    {
        public new FieldVerificationProvider<string, ValidationMessage<TOwner>, TOwner> Should => new(this);
    }

    public static class ValidationMessageExtensions
    {
        public static TOwner BeRequiredError<TOwner>(this IFieldVerificationProvider<string, ValidationMessage<TOwner>, TOwner> should)
            where TOwner : PageObject<TOwner> =>
            should.Contain("required");

        public static TOwner BeInvalidError<TOwner>(this IFieldVerificationProvider<string, ValidationMessage<TOwner>, TOwner> should)
            where TOwner : PageObject<TOwner> =>
            should.Contain("invalid");
    }
}

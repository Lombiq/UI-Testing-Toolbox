namespace Lombiq.Tests.UI.MonkeyTesting
{
    /// <summary>
    /// URL sanitizer that removes specific query parameter.
    /// </summary>
    public class RemovesQueryParameterMonkeyTestingUrlSanitizer : RemovesByRegexMonkeyTestingUrlSanitizer
    {
        public RemovesQueryParameterMonkeyTestingUrlSanitizer(string parameterName)
            : base(@$"(\b{parameterName}=[^&]*&|[\?&]{parameterName}=[^&]*$)")
        {
        }
    }
}

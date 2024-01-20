namespace Lombiq.Tests.UI.MonkeyTesting.UrlSanitizers;

/// <summary>
/// URL sanitizer that removes specific query parameter.
/// </summary>
public class RemovesQueryParameterMonkeyTestingUrlSanitizer(string parameterName)
    : RemovesByRegexMonkeyTestingUrlSanitizer(@$"(\b{parameterName}=[^&]*&|[\?&]{parameterName}=[^&]*$)")
{
}

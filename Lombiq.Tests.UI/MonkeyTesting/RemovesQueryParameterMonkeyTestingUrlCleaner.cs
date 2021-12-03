namespace Lombiq.Tests.UI.MonkeyTesting
{
    public class RemovesQueryParameterMonkeyTestingUrlCleaner : RemovesByRegexMonkeyTestingUrlCleaner
    {
        public RemovesQueryParameterMonkeyTestingUrlCleaner(string parameterName)
            : base(@$"(\b{parameterName}=[^&]*&|[\?&]{parameterName}=[^&]*$)")
        {
        }
    }
}

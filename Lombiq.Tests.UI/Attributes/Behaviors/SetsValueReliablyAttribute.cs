using Atata;
using Lombiq.Tests.UI.Helpers;
using OpenQA.Selenium;
using System;

namespace Lombiq.Tests.UI.Attributes.Behaviors
{
    public sealed class SetsValueReliablyAttribute : ValueSetBehaviorAttribute
    {
        public override void Execute<TOwner>(IUIComponent<TOwner> component, string value)
        {
            var element = component.Scope;

            ReliabilityHelper.DoWithRetriesOrFail(
                () => SetValue(element, value).GetValue() == value);
        }

        private static IWebElement SetValue(IWebElement element, string text)
        {
            element.ClearWithLogging();

            if (text.Contains('@', StringComparison.Ordinal))
            {
                // On some platforms, probably due to keyboard settings, the @ character can be missing from the address
                // when entered into a text field so we need to use Actions. The following solution doesn't work:
                // https://stackoverflow.com/a/52202594/220230. This needs to be done in addition to the standard
                // FillInWith() as without that some forms start to behave strange and not save values.
                AtataContext.Current.Driver.Perform(actions => actions.SendKeys(element, text));
            }
            else
            {
                element.SendKeysWithLogging(text);
            }

            return element;
        }
    }
}

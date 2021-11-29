using Atata;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;

namespace Lombiq.Tests.UI.Extensions
{
    public static class EmailUITestContextExtensions
    {
        public static IWebElement FindSpecificEmailInInbox(
            this UITestContext context,
            string emailTitle,
            string textToFind)
        {
            context.GoToSmtpWebUI();
            context.ClickReliablyOn(ByHelper.SmtpInboxRow(emailTitle));
            context.SwitchToFrame0();

            var currentlySelectedEmail = context.Get(By.CssSelector(".emailContent p"));
            while (!currentlySelectedEmail.Text.Contains(textToFind, StringComparison.InvariantCultureIgnoreCase))
            {
                context.SwitchToFirstWindow();
                context.ClickReliablyOn(By.CssSelector(".unread").Within(TimeSpan.FromMinutes(2)));
                context.SwitchToFrame0();

                currentlySelectedEmail = context.Get(By.CssSelector(".emailContent p"));
            }

            return currentlySelectedEmail;
        }
    }
}

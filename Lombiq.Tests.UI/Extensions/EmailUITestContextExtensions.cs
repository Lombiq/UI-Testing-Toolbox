using Atata;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using Shouldly;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class EmailUITestContextExtensions
{
    /// <summary>
    /// Navigates to the smtp4dev web UI that is launched if <see
    /// cref="OrchardCoreUITestExecutorConfiguration.UseSmtpService"/> is set to <see langword="true"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the smtp4dev server is not running.</exception>
    public static Task GoToSmtpWebUIAsync(this UITestContext context)
    {
        if (context.SmtpServiceRunningContext == null)
        {
            throw new InvalidOperationException(
                "The SMTP service is not running. Did you turn it on with " +
                nameof(OrchardCoreUITestExecutorConfiguration) + "." + nameof(OrchardCoreUITestExecutorConfiguration.UseSmtpService) +
                " and could it properly start?");
        }

        return context.GoToAbsoluteUrlAsync(context.SmtpServiceRunningContext.WebUIUri);
    }

    /// <summary>
    /// Finds and leaves open the first email in the smtp4dev Web UI whose title contains <paramref name="emailTitle"/>
    /// and message body contains <paramref name="textToFind"/>. If none are found <see cref="NotFoundException"/> is
    /// thrown.
    /// </summary>
    public static async Task<IWebElement> FindSpecificEmailInInboxAsync(
        this UITestContext context,
        string emailTitle,
        string textToFind)
    {
        await context.GoToSmtpWebUIAsync();
        await context.ClickReliablyOnAsync(ByHelper.SmtpInboxRow(emailTitle));
        context.SwitchToFrame0();

        var currentlySelectedEmail = context.Get(By.CssSelector(".emailContent p"));
        while (!currentlySelectedEmail.Text.Contains(textToFind, StringComparison.InvariantCultureIgnoreCase))
        {
            context.SwitchToFirstWindow();
            await context.ClickReliablyOnAsync(By.CssSelector(".unread").Within(TimeSpan.FromMinutes(2)));
            context.SwitchToFrame0();

            currentlySelectedEmail = context.Get(By.CssSelector(".emailContent p"));
        }

        return currentlySelectedEmail;
    }

    /// <summary>
    /// Navigates to the <c>/Admin/Settings/email</c> page.
    /// </summary>
    public static Task GoToEmailSettingsAsync(this UITestContext context) =>
        context.GoToAdminRelativeUrlAsync("/Settings/email");

    /// <summary>
    /// Navigates to the <c>/Admin/Email/Test</c> page.
    /// </summary>
    public static Task GoToEmailTestAsync(this UITestContext context) =>
        context.GoToAdminRelativeUrlAsync("/Email/Test");

    /// <summary>
    /// Fills out the form on the email test page by specifying the recipient address, subject and message body. If the
    /// <paramref name="submit"/> is <see langword="true"/>, it also clicks on the send button.
    /// </summary>
    public static async Task FillEmailTestFormAsync(
        this UITestContext context,
        string to,
        string subject,
        string body,
        bool submit = true)
    {
        await context.FillInWithRetriesAsync(By.Id("To"), to);
        await context.FillInWithRetriesAsync(By.Id("Subject"), subject);
        await context.FillInWithRetriesAsync(By.Id("Body"), body);

        if (submit) await context.ClickReliablyOnSubmitAsync();
    }

    /// <summary>
    /// A simplified version of <see cref="FillEmailTestFormAsync(UITestContext,string,string,string,bool)"/> where the
    /// sender if <c>"recipient@example.com"</c> and the message body is <c>"Hi, this is a test."</c>.
    /// </summary>
    public static Task FillEmailTestFormAsync(this UITestContext context, string subject) =>
        context.FillEmailTestFormAsync("recipient@example.com", subject, "Hi, this is a test.");

    [Obsolete("Use ConfigureSmtpSettingsAsync() instead.")]
    public static Task ConfigureSmtpPortAsync(this UITestContext context, int? port = null, bool publish = true) =>
        throw new NotSupportedException("Use ConfigureSmtpSettingsAsync() instead.");

    /// <summary>
    /// Goes to the SMTP settings page and configures the provided settings. The <c>OrchardCore.Email.Smtp</c> feature
    /// must be enabled, but if the SMTP provider is not turned on, this will automatically do it as well.
    /// </summary>
    /// <param name="host">The SMTP host to use.</param>
    /// <param name="port">The SMTP port to use. If it's <see langword="null"/> then the value in the current
    /// configuration (in <see cref="OrchardCoreUITestExecutorConfiguration.SmtpServiceConfiguration"/>) is used
    /// instead.</param>
    /// <param name="save">Whether to save the settings after configuring them.</param>
    public static async Task ConfigureSmtpSettingsAsync(
        this UITestContext context,
        string defaultSender,
        string host,
        int? port = null,
        bool save = true)
    {
        await context.GoToEmailSettingsAsync();
        await context.ClickReliablyOnAsync(By.CssSelector("a[href='#tab-s-m-t-p']"));

        var byIsEnabled = By.Id("ISite_SmtpSettings_IsEnabled").OfAnyVisibility();
        if (context.Get(byIsEnabled).GetAttribute("checked") == null)
        {
            await context.SetCheckboxValueAsync(byIsEnabled, isChecked: true);
        }

        port ??= context.Configuration?.SmtpServiceConfiguration?.Context?.Port;
        if (!port.HasValue)
        {
            throw new InvalidOperationException(
                "The SMTP port configuration is missing. Did you forget to include \"configuration.UseSmtpService = true\"?");
        }

        await context.ClickAndFillInWithRetriesAsync(By.Id("ISite_SmtpSettings_DefaultSender"), defaultSender);
        await context.ClickAndFillInWithRetriesAsync(By.Id("ISite_SmtpSettings_Host"), host);

        var smtpPort = port.Value.ToTechnicalString();
        await context.ClickAndFillInWithRetriesAsync(By.Id("ISite_SmtpSettings_Port"), smtpPort);

        if (save)
        {
            await context.ClickReliablyOnAsync(By.ClassName("save"));
            context.Get(By.ClassName("validation-summary-errors").Safely())?.Text?.Trim().ShouldBeNullOrEmpty();
        }
    }
}

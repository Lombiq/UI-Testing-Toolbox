using Atata;
using Lombiq.Tests.UI.Services;
using Microsoft.SqlServer.Management.Dmf;
using OpenQA.Selenium;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class ExtendedLoggingExtensions
{
    /// <summary>
    /// Used for edge cases like when a scope becomes stale
    /// </summary>
    private const int StabilityRetryCount = 3;

    public static Task ExecuteLoggedAsync(
        this UITestContext context, string operationName, IWebElement element, Func<Task> functionAsync) =>
        context.ExecuteSectionAsync(GetLogSection(operationName, element), functionAsync);

    public static Task<TResult> ExecuteLoggedAsync<TResult>(
        this UITestContext context, string operationName, IWebElement element, Func<Task<TResult>> functionAsync) =>
        context.ExecuteSectionAsync(GetLogSection(operationName, element), functionAsync);

    public static Task ExecuteLoggedAsync(
        this UITestContext context, string operationName, By by, Func<Task> functionAsync) =>
        context.ExecuteSectionAsync(GetLogSection(operationName, by), functionAsync);

    public static Task<TResult> ExecuteLoggedAsync<TResult>(
        this UITestContext context, string operationName, By by, Func<Task<TResult>> functionAsync) =>
        context.ExecuteSectionAsync(GetLogSection(operationName, by), functionAsync);

    public static Task ExecuteLoggedAsync(
        this UITestContext context, string operationName, string objectOfOperation, Func<Task> functionAsync) =>
        context.ExecuteSectionAsync(GetLogSection(operationName, objectOfOperation), functionAsync);

    public static Task<TResult> ExecuteLoggedAsync<TResult>(
        this UITestContext context, string operationName, string objectOfOperation, Func<Task<TResult>> functionAsync) =>
        context.ExecuteSectionAsync(GetLogSection(operationName, objectOfOperation), functionAsync);

    public static Task ExecuteLoggedAsync(
        this UITestContext context, string operationName, Func<Task> functionAsync) =>
        context.ExecuteSectionAsync(GetLogSection(operationName), functionAsync);

    public static Task<TResult> ExecuteLoggedAsync<TResult>(
        this UITestContext context, string operationName, Func<Task<TResult>> functionAsync) =>
        context.ExecuteSectionAsync(GetLogSection(operationName), functionAsync);

    public static void ExecuteLogged(
        this UITestContext context, string operationName, IWebElement element, Action action) =>
        context.ExecuteSection(GetLogSection(operationName, element), action);

    public static TResult ExecuteLogged<TResult>(
        this UITestContext context, string operationName, IWebElement element, Func<TResult> function) =>
        context.ExecuteSection(GetLogSection(operationName, element), function);

    public static void ExecuteLogged(
        this UITestContext context, string operationName, By by, Action action) =>
        context.ExecuteSection(GetLogSection(operationName, by), action);

    public static TResult ExecuteLogged<TResult>(
        this UITestContext context, string operationName, By by, Func<TResult> function) =>
        context.ExecuteSection(GetLogSection(operationName, by), function);

    public static void ExecuteLogged(
        this UITestContext context, string operationName, string objectOfOperation, Action action) =>
        context.ExecuteSection(GetLogSection(operationName, objectOfOperation), action);

    public static TResult ExecuteLogged<TResult>(
        this UITestContext context, string operationName, string objectOfOperation, Func<TResult> function) =>
        context.ExecuteSection(GetLogSection(operationName, objectOfOperation), function);

    public static void ExecuteLogged(
        this UITestContext context, string operationName, Action action) =>
        context.ExecuteSection(GetLogSection(operationName), action);

    public static TResult ExecuteLogged<TResult>(
        this UITestContext context, string operationName, Func<TResult> function) =>
        context.ExecuteSection(GetLogSection(operationName), function);

    private static LogSection GetLogSection(string operationName, IWebElement element) =>
        new(GetSectionMessage(operationName, element.ToDetailedString()));

    private static LogSection GetLogSection(string operationName, By by) =>
        new(GetSectionMessage(operationName, by.ToString()));

    private static LogSection GetLogSection(string operationName, string objectOfOperation) =>
        new(GetSectionMessage(operationName, objectOfOperation));

    private static LogSection GetLogSection(string operationName) => new(operationName);

    private static string GetSectionMessage(string operationName, string objectOfOperation) =>
        $"{operationName} applied to: {Environment.NewLine}{objectOfOperation}";

    private static void ExecuteSection(this UITestContext context, LogSection section, Action action) =>
        context.Scope.AtataContext.Log.ExecuteSection(section, () =>
        {
            for (int i = 0; i < StabilityRetryCount; i++)
            {
                var notLast = i < StabilityRetryCount - 1;
                try
                {
                    action();
                    return;
                }
                catch (StaleElementReferenceException) when (notLast)
                {
                    Task.Delay(TimeSpan.FromSeconds(1)).Wait();
                }
            }
        });

    private static TResult ExecuteSection<TResult>(this UITestContext context, LogSection section, Func<TResult> function) =>
        context.Scope.AtataContext.Log.ExecuteSection(section, () =>
        {
            for (int i = 0; i < StabilityRetryCount; i++)
            {
                var notLast = i < StabilityRetryCount - 1;
                try
                {
                    return function();
                }
                catch (StaleElementReferenceException) when (notLast)
                {
                    Task.Delay(TimeSpan.FromSeconds(1)).Wait();
                }
            }

            throw new InvalidOperandException("Impossible to reach.");
        });

    private static Task ExecuteSectionAsync(this UITestContext context, LogSection section, Func<Task> functionAsync) =>
        context.ExecuteSectionAsync(
            section,
            async () =>
            {
                await functionAsync();
                return true;
            });

    private static async Task<TResult> ExecuteSectionAsync<TResult>(
        this UITestContext context, LogSection section, Func<Task<TResult>> functionAsync)
    {
        for (int i = 0; i < StabilityRetryCount; i++)
        {
            var notLast = i < StabilityRetryCount - 1;
            try
            {
                // This is somewhat risky. ILogManager is not thread-safe and uses as stack to keep track of sections, so if
                // multiple sections are started in concurrent threads, the result will be incorrect. This shouldn't be too much
                // of an issue for now though since tests, while async, are single-threaded.
                context.Scope.AtataContext.Log.Start(section);
                var result = await functionAsync();
                context.Scope.AtataContext.Log.EndSection();
                return result;
            }
            catch (StaleElementReferenceException) when (notLast)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        throw new InvalidOperandException("Impossible to reach.");
    }
}

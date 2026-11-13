using Atata;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class ExtendedLoggingExtensions
{
    [StackTraceHidden]
    public static Task ExecuteLoggedAsync(
        this UITestContext context, string operationName, IWebElement element, Func<Task> functionAsync) =>
        context.ExecuteSectionAsync(GetLogSection(operationName, element), functionAsync);

    [StackTraceHidden]
    public static Task<TResult> ExecuteLoggedAsync<TResult>(
        this UITestContext context, string operationName, IWebElement element, Func<Task<TResult>> functionAsync) =>
        context.ExecuteSectionAsync(GetLogSection(operationName, element), functionAsync);

    [StackTraceHidden]
    public static Task ExecuteLoggedAsync(
        this UITestContext context, string operationName, By by, Func<Task> functionAsync) =>
        context.ExecuteSectionAsync(GetLogSection(operationName, by), functionAsync);

    [StackTraceHidden]
    public static Task<TResult> ExecuteLoggedAsync<TResult>(
        this UITestContext context, string operationName, By by, Func<Task<TResult>> functionAsync) =>
        context.ExecuteSectionAsync(GetLogSection(operationName, by), functionAsync);

    [StackTraceHidden]
    public static Task ExecuteLoggedAsync(
        this UITestContext context, string operationName, string objectOfOperation, Func<Task> functionAsync) =>
        context.ExecuteSectionAsync(GetLogSection(operationName, objectOfOperation), functionAsync);

    [StackTraceHidden]
    public static Task<TResult> ExecuteLoggedAsync<TResult>(
        this UITestContext context, string operationName, string objectOfOperation, Func<Task<TResult>> functionAsync) =>
        context.ExecuteSectionAsync(GetLogSection(operationName, objectOfOperation), functionAsync);

    [StackTraceHidden]
    public static Task ExecuteLoggedAsync(
        this UITestContext context, string operationName, Func<Task> functionAsync) =>
        context.ExecuteSectionAsync(GetLogSection(operationName), functionAsync);

    [StackTraceHidden]
    public static Task<TResult> ExecuteLoggedAsync<TResult>(
        this UITestContext context, string operationName, Func<Task<TResult>> functionAsync) =>
        context.ExecuteSectionAsync(GetLogSection(operationName), functionAsync);

    [StackTraceHidden]
    public static void ExecuteLogged(
        this UITestContext context, string operationName, IWebElement element, Action action) =>
        context.ExecuteSection(GetLogSection(operationName, element), action);

    [StackTraceHidden]
    public static TResult ExecuteLogged<TResult>(
        this UITestContext context, string operationName, IWebElement element, Func<TResult> function) =>
        context.ExecuteSection(GetLogSection(operationName, element), function);

    [StackTraceHidden]
    public static void ExecuteLogged(
        this UITestContext context, string operationName, By by, Action action) =>
        context.ExecuteSection(GetLogSection(operationName, by), action);

    [StackTraceHidden]
    public static TResult ExecuteLogged<TResult>(
        this UITestContext context, string operationName, By by, Func<TResult> function) =>
        context.ExecuteSection(GetLogSection(operationName, by), function);

    [StackTraceHidden]
    public static void ExecuteLogged(
        this UITestContext context, string operationName, string objectOfOperation, Action action) =>
        context.ExecuteSection(GetLogSection(operationName, objectOfOperation), action);

    [StackTraceHidden]
    public static TResult ExecuteLogged<TResult>(
        this UITestContext context, string operationName, string objectOfOperation, Func<TResult> function) =>
        context.ExecuteSection(GetLogSection(operationName, objectOfOperation), function);

    [StackTraceHidden]
    public static void ExecuteLogged(
        this UITestContext context, string operationName, Action action) =>
        context.ExecuteSection(GetLogSection(operationName), action);

    [StackTraceHidden]
    public static TResult ExecuteLogged<TResult>(
        this UITestContext context, string operationName, Func<TResult> function) =>
        context.ExecuteSection(GetLogSection(operationName), function);

    [StackTraceHidden]
    private static LogSection GetLogSection(string operationName, IWebElement element) =>
        new(GetSectionMessage(operationName, element.ToDetailedString()));

    [StackTraceHidden]
    private static LogSection GetLogSection(string operationName, By by) =>
        new(GetSectionMessage(operationName, by.ToString()));

    [StackTraceHidden]
    private static LogSection GetLogSection(string operationName, string objectOfOperation) =>
        new(GetSectionMessage(operationName, objectOfOperation));

    [StackTraceHidden]
    private static LogSection GetLogSection(string operationName) => new(operationName);

    [StackTraceHidden]
    private static string GetSectionMessage(string operationName, string objectOfOperation) =>
        $"{operationName} applied to: {Environment.NewLine}{objectOfOperation}";

    [StackTraceHidden]
    private static void ExecuteSection(this UITestContext context, LogSection section, Action action) =>
       context.Scope.AtataContext.Log.ExecuteSection(section, action);

    [StackTraceHidden]
    private static TResult ExecuteSection<TResult>(this UITestContext context, LogSection section, Func<TResult> function) =>
        context.Scope.AtataContext.Log.ExecuteSection(section, function);

    [StackTraceHidden]
    private static Task<bool> ExecuteSectionAsync(this UITestContext context, LogSection section, Func<Task> functionAsync) =>
        context.ExecuteSectionAsync(
            section,
            async () =>
            {
                await functionAsync();
                return true;
            });

    [StackTraceHidden]
    private static Task<TResult> ExecuteSectionAsync<TResult>(
        this UITestContext context, LogSection section, Func<Task<TResult>> functionAsync) =>
        context.Scope.AtataContext.Log.ExecuteSectionAsync(
            section,
            async () =>
            {
                context.Scope.AtataContext.Log.Info($"Log section {section.Message} started.");
                var result = await functionAsync();
                context.Scope.AtataContext.Log.Info($"Log section {section.Message} ended.");
                return result;
            });
}

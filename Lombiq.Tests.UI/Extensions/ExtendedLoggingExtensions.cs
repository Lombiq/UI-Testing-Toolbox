using Atata;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;

namespace Lombiq.Tests.UI.Extensions
{
    public static class ExtendedLoggingExtensions
    {
        public static void ExecuteLogged(this UITestContext context, string operationName, IWebElement element, Action action) =>
            context.ExecuteSection(GetLogSection(operationName, element), action);

        public static TResult ExecuteLogged<TResult>(this UITestContext context, string operationName, IWebElement element, Func<TResult> function) =>
            context.ExecuteSection(GetLogSection(operationName, element), function);

        public static void ExecuteLogged(this UITestContext context, string operationName, By by, Action action) =>
            context.ExecuteSection(GetLogSection(operationName, by), action);

        public static TResult ExecuteLogged<TResult>(this UITestContext context, string operationName, By by, Func<TResult> function) =>
            context.ExecuteSection(GetLogSection(operationName, by), function);

        public static void ExecuteLogged(this UITestContext context, string operationName, string objectOfOperation, Action action) =>
            context.ExecuteSection(GetLogSection(operationName, objectOfOperation), action);

        public static TResult ExecuteLogged<TResult>(this UITestContext context, string operationName, string objectOfOperation, Func<TResult> function) =>
            context.ExecuteSection(GetLogSection(operationName, objectOfOperation), function);

        private static LogSection GetLogSection(string operationName, IWebElement element) =>
            new LogSection(GetSectionMessage(operationName, element.ToDetailedString()));

        private static LogSection GetLogSection(string operationName, By by) =>
            new LogSection(GetSectionMessage(operationName, by.ToString()));

        private static LogSection GetLogSection(string operationName, string objectOfOperation) =>
            new LogSection(GetSectionMessage(operationName, objectOfOperation));

        private static string GetSectionMessage(string operationName, string objectOfOperation) =>
            $"{operationName} applied to: {Environment.NewLine}{objectOfOperation}";

        private static void ExecuteSection(this UITestContext context, LogSection section, Action action) =>
            context.Scope.AtataContext.Log.ExecuteSection(section, action);

        private static TResult ExecuteSection<TResult>(this UITestContext context, LogSection section, Func<TResult> function) =>
            context.Scope.AtataContext.Log.ExecuteSection(section, function);
    }
}

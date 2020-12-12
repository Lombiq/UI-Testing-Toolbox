using Atata;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;
using System.Diagnostics;

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

        private static ExtendedLogSection GetLogSection(string operationName, IWebElement element) =>
            new ExtendedLogSection(GetSectionMessage(operationName, element.ToDetailedString()));

        private static ExtendedLogSection GetLogSection(string operationName, By by) =>
            new ExtendedLogSection(GetSectionMessage(operationName, by.ToString()));

        private static ExtendedLogSection GetLogSection(string operationName, string objectOfOperation) =>
            new ExtendedLogSection(GetSectionMessage(operationName, objectOfOperation));

        private static string GetSectionMessage(string operationName, string objectOfOperation) =>
            $"{operationName} applied to: {Environment.NewLine}{objectOfOperation}";

        #region ExecuteSectionBackPort
        // Only needed until this change of Atata is released:
        // https://github.com/atata-framework/atata/commit/bf9a9cf9d665a1e790b0efcd1a98c23cc787417f. Copied from the
        // Atata code base, stripped down to not include what's needed for other new features there.
        private static void ExecuteSection(this UITestContext context, ExtendedLogSection section, Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            var logManager = context.Scope.AtataContext.Log;

            logManager.Start(section);

            try
            {
                action?.Invoke();
            }
            catch (Exception exception)
            {
                section.Exception = exception;
                throw;
            }
            finally
            {
                logManager.EndSection();
            }
        }

        private static TResult ExecuteSection<TResult>(this UITestContext context, ExtendedLogSection section, Func<TResult> function)
        {
            if (function == null) throw new ArgumentNullException(nameof(function));

            var logManager = context.Scope.AtataContext.Log;

            logManager.Start(section);

            try
            {
                return function.Invoke();
            }
            catch (Exception exception)
            {
                section.Exception = exception;
                throw;
            }
            finally
            {
                logManager.EndSection();
            }
        }

        private class ExtendedLogSection : LogSection
        {
            public ExtendedLogSection(string message = null, Atata.LogLevel level = Atata.LogLevel.Info)
                : base(message, level)
            {
            }

            /// <summary>
            /// Gets the date/time of section start.
            /// </summary>
            internal Stopwatch Stopwatch { get; } = new Stopwatch();

            /// <summary>
            /// Gets or sets the exception.
            /// </summary>
            public Exception Exception { get; set; }
        }
        #endregion ExecuteSectionBackPort
    }
}

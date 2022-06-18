using Atata;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class ReliabilityUITestContextExtensions
{
    /// <summary>
    /// Executes the process repeatedly while it's not successful, with the given timeout and retry intervals. If the
    /// operation didn't succeed then throws a <see cref="TimeoutException"/>.
    /// </summary>
    /// <param name="process">
    /// The operation that potentially needs to be retried. Should return <see langword="true"/> if it's successful,
    /// <see langword="false"/> otherwise.
    /// </param>
    /// <param name="timeout">
    /// The maximum time allowed for the process to complete. Defaults to <paramref
    /// name="context.Configuration.TimeoutConfiguration.RetryTimeout"/>.
    /// </param>
    /// <param name="interval">
    /// The polling interval used by <see cref="SafeWait{T}"/>. Defaults to <paramref
    /// name="context.Configuration.TimeoutConfiguration.RetryInterval"/>.
    /// </param>
    /// <exception cref="TimeoutException">
    /// Thrown if the operation didn't succeed even after retries within the allotted time.
    /// </exception>
    public static void DoWithRetriesOrFail(
        this UITestContext context,
        Func<bool> process,
        TimeSpan? timeout = null,
        TimeSpan? interval = null) =>
        ReliabilityHelper.DoWithRetriesOrFail(
            process,
            timeout ?? context.Configuration.TimeoutConfiguration.RetryTimeout,
            interval ?? context.Configuration.TimeoutConfiguration.RetryInterval);

    /// <summary>
    /// Executes the async process repeatedly while it's not successful, with the given timeout and retry intervals. If
    /// the operation didn't succeed then throws a <see cref="TimeoutException"/>.
    /// </summary>
    /// <param name="processAsync">
    /// The operation that potentially needs to be retried. Should return <see langword="true"/> if it's successful,
    /// <see langword="false"/> otherwise.
    /// </param>
    /// <param name="timeout">
    /// The maximum time allowed for the process to complete. Defaults to <paramref
    /// name="context.Configuration.TimeoutConfiguration.RetryTimeout"/>.
    /// </param>
    /// <param name="interval">
    /// The polling interval used by <see cref="SafeWaitAsync{T}"/>. Defaults to <paramref
    /// name="context.Configuration.TimeoutConfiguration.RetryInterval"/>.
    /// </param>
    /// <exception cref="TimeoutException">
    /// Thrown if the operation didn't succeed even after retries within the allotted time.
    /// </exception>
    public static Task DoWithRetriesOrFailAsync(
        this UITestContext context,
        Func<Task<bool>> processAsync,
        TimeSpan? timeout = null,
        TimeSpan? interval = null) =>
        ReliabilityHelper.DoWithRetriesOrFailAsync(
            processAsync,
            timeout ?? context.Configuration.TimeoutConfiguration.RetryTimeout,
            interval ?? context.Configuration.TimeoutConfiguration.RetryInterval);

    /// <summary>
    /// Executes the async process and retries if an element becomes stale ( <see
    /// cref="StaleElementReferenceException"/>). If the operation didn't succeed then throws a <see
    /// cref="TimeoutException"/>.
    ///
    /// In situations like a DataTable load it is possible that the page will change during execution of multiple long
    /// running operations such as GetAll, causing stale virtual DOM. Such change tends to be near instantaneous and
    /// only happens once at a time so this should pass by the 2nd try.
    /// </summary>
    /// <param name="processAsync">
    /// The long running operation that may execute during DOM change and should be retried. Should return <see
    /// langword="true"/> if no retries are necessary, throw <see cref="StaleElementReferenceException"/> or return <see
    /// langword="false"/> otherwise.
    /// </param>
    /// <param name="timeout">
    /// The maximum time allowed for the process to complete. Defaults to <paramref
    /// name="context.Configuration.TimeoutConfiguration.RetryTimeout"/>.
    /// </param>
    /// <param name="interval">
    /// The polling interval used by <see cref="SafeWaitAsync{T}"/>. Defaults to <paramref
    /// name="context.Configuration.TimeoutConfiguration.RetryInterval"/>.
    /// </param>
    /// <exception cref="TimeoutException">
    /// Thrown if the operation didn't succeed even after retries within the allotted time.
    /// </exception>
    public static Task RetryIfStaleOrFailAsync(
        this UITestContext context,
        Func<Task<bool>> processAsync,
        TimeSpan? timeout = null,
        TimeSpan? interval = null) =>
        ReliabilityHelper.RetryIfStaleOrFailAsync(
            processAsync,
            timeout ?? context.Configuration.TimeoutConfiguration.RetryTimeout,
            interval ?? context.Configuration.TimeoutConfiguration.RetryInterval);

    /// <summary>
    /// Executes the process and retries until no element is stale ( <see cref="StaleElementReferenceException"/>).
    ///
    /// If the operation didn't succeed then throws a <see cref="TimeoutException"/>.
    /// </summary>
    /// <param name="processAsync">
    /// The long running operation that may execute during DOM change and should be retried. Should return <see
    /// langword="true"/> if no retries are necessary, throw <see cref="StaleElementReferenceException"/> or return <see
    /// langword="false"/> otherwise.
    /// </param>
    /// <param name="timeout">
    /// The maximum time allowed for the process to complete. Defaults to <paramref
    /// name="context.Configuration.TimeoutConfiguration.RetryTimeout"/>.
    /// </param>
    /// <param name="interval">
    /// The polling interval used by <see cref="SafeWaitAsync{T}"/>. Defaults to <paramref
    /// name="context.Configuration.TimeoutConfiguration.RetryInterval"/>.
    /// </param>
    /// <exception cref="TimeoutException">
    /// Thrown if the operation didn't succeed even after retries within the allotted time.
    /// </exception>
    public static Task RetryIfNotStaleOrFailAsync(
        this UITestContext context,
        Func<Task<bool>> processAsync,
        TimeSpan? timeout = null,
        TimeSpan? interval = null) =>
        ReliabilityHelper.RetryIfNotStaleOrFailAsync(
            processAsync,
            timeout ?? context.Configuration.TimeoutConfiguration.RetryTimeout,
            interval ?? context.Configuration.TimeoutConfiguration.RetryInterval);

    /// <summary>
    /// Tries to execute an operation until the given element exists.
    /// </summary>
    /// <param name="processAsync">Operation to execute.</param>
    /// <param name="elementToWaitFor">Selector of the element that's required to exist.</param>
    /// <param name="timeout">Timeout of the operation.</param>
    /// <param name="interval">Time between retries.</param>
    /// <param name="existsTimeout">Timeout of checking the existence of the given element.</param>
    public static Task DoWithRetriesUntilExistsAsync(
        this UITestContext context,
        Func<Task> processAsync,
        By elementToWaitFor,
        TimeSpan? timeout = null,
        TimeSpan? interval = null,
        TimeSpan? existsTimeout = null) =>
        context.DoWithRetriesOrFailAsync(
            async () =>
            {
                await processAsync();

                existsTimeout ??= GetExistsTimeout(context, timeout);

                return ExistsWithin(context, elementToWaitFor, existsTimeout.Value, interval);
            },
            timeout,
            interval);

    /// <summary>
    /// Tries to execute an operation until the given element goes missing.
    /// </summary>
    /// <param name="processAsync">Operation to execute.</param>
    /// <param name="elementToWaitForGoMissing">Selector of the element that's required to go missing.</param>
    /// <param name="timeout">Timeout of the operation.</param>
    /// <param name="interval">Time between retries.</param>
    /// <param name="existsTimeout">Timeout of checking the existence of the given element.</param>
    public static Task DoWithRetriesUntilMissingAsync(
        this UITestContext context,
        Func<Task> processAsync,
        By elementToWaitForGoMissing,
        TimeSpan? timeout = null,
        TimeSpan? interval = null,
        TimeSpan? existsTimeout = null) =>
        context.DoWithRetriesOrFailAsync(
            async () =>
            {
                await processAsync();

                existsTimeout ??= GetExistsTimeout(context, timeout);

                return MissingWithin(context, elementToWaitForGoMissing, existsTimeout.Value, interval);
            },
            timeout,
            interval);

    /// <summary>
    /// Waits until the element to be ready, to avoid animation related issues.
    /// </summary>
    /// <param name="elementToWait">Selector of the element that's required to be ready.</param>
    /// <param name="timeout">Timeout of the operation.</param>
    /// <param name="interval">Time between retries.</param>
    /// <returns>Hash calculated from captured screen shot of element region.</returns>
    public static string WaitElementToNotChange(
        this UITestContext context,
        By elementToWait,
        TimeSpan? timeout = null,
        TimeSpan? interval = null)
    {
        string lastHash = null;
        context.DoWithRetriesOrFail(
            () =>
            {
                var element = context.Get(elementToWait);
                var hash = context.ComputeElementImageHash(element);

                var ready = hash == lastHash;

                lastHash = hash;

                return ready;
            },
            timeout,
            interval);

        return lastHash;
    }

    /// <summary>
    /// Waits until the scrolling to be ready.
    /// </summary>
    /// <param name="timeout">Timeout of the operation.</param>
    /// <param name="interval">Time between retries.</param>
    public static void WaitScrollToNotChange(
        this UITestContext context,
        TimeSpan? timeout = null,
        TimeSpan? interval = null)
    {
        var lastScrollPosition = context.GetScrollPosition();
        Thread.Sleep(interval ?? TimeSpan.FromMilliseconds(500));
        context.DoWithRetriesOrFail(
            () =>
            {
                var currentScrollPosition = context.GetScrollPosition();

                var ready = lastScrollPosition == currentScrollPosition;

                lastScrollPosition = currentScrollPosition;

                return ready;
            },
            timeout,
            interval);
    }

    private static TimeSpan GetExistsTimeout(UITestContext context, TimeSpan? timeout) =>
        // The timeout for this existence check needs to be significantly smaller than the timeout of the whole retry
        // logic so actually multiple tries can happen.
        (timeout ?? context.Configuration.TimeoutConfiguration.RetryTimeout) / 5;

    private static bool ExistsWithin(
        UITestContext context,
        By elementToWaitFor,
        TimeSpan existsTimeout,
        TimeSpan? interval = null) =>
        context.Exists(elementToWaitFor.Safely().Within(
            existsTimeout,
            interval ?? context.Configuration.TimeoutConfiguration.RetryInterval));

    private static bool MissingWithin(
        UITestContext context,
        By elementToWaitForGoMissing,
        TimeSpan existsTimeout,
        TimeSpan? interval = null) =>
        context.Missing(elementToWaitForGoMissing.Safely().Within(
            existsTimeout,
            interval ?? context.Configuration.TimeoutConfiguration.RetryInterval));

    private static string ComputeElementImageHash(this UITestContext context, IWebElement element)
    {
        using var elementImage = context.TakeElementScreenshot(element);
        using var elementImageStream = new MemoryStream();

        elementImage.Save(elementImageStream, ImageFormat.Png);
        return ComputeSha256Hash(elementImageStream);
    }

    private static string ComputeSha256Hash(Stream stream)
    {
        using var sha256Hash = SHA256.Create();

        return string.Concat(
            sha256Hash.ComputeHash(stream)
                .Select(item => item.ToString("x2", CultureInfo.InvariantCulture)));
    }
}

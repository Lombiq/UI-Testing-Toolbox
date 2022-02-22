using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Atata
{
    /// <summary>
    /// Represents the retriable operation to wait for async condition safely (without throwing exception on timeout).
    /// </summary>
    /// <typeparam name="T">The type of object used to detect the condition.</typeparam>
    public class SafeWaitAsync<T> : IWait<T>
    {
        private readonly T _input;

        private readonly IClock _clock;

        private readonly List<Type> _ignoredExceptions = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="SafeWaitAsync{T}"/> class.
        /// </summary>
        /// <param name="input">The input value to pass to the evaluated conditions.</param>
        public SafeWaitAsync(T input)
            : this(input, new SystemClock())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SafeWaitAsync{T}"/> class.
        /// </summary>
        /// <param name="input">The input value to pass to the evaluated conditions.</param>
        /// <param name="clock">The clock to use when measuring the timeout.</param>
        public SafeWaitAsync(T input, IClock clock)
        {
            _input = input.CheckNotNull(nameof(input));
            _clock = clock.CheckNotNull(nameof(clock));
        }

        /// <summary>
        /// Gets or sets how long to wait for the evaluated condition to be true.
        /// The default timeout is taken from <see cref="RetrySettings.Timeout"/>.
        /// </summary>
        public TimeSpan Timeout { get; set; } = RetrySettings.Timeout;

        /// <summary>
        /// Gets or sets how often the condition should be evaluated.
        /// The default interval is taken from <see cref="RetrySettings.Interval"/>.
        /// </summary>
        public TimeSpan PollingInterval { get; set; } = RetrySettings.Interval;

        public string Message { get; set; }

        /// <summary>
        /// Configures this instance to ignore specific types of exceptions while waiting for a condition.
        /// Any exceptions not whitelisted will be allowed to propagate, terminating the wait.
        /// </summary>
        /// <param name="exceptionTypes">The types of exceptions to ignore.</param>
        public void IgnoreExceptionTypes(params Type[] exceptionTypes)
        {
            exceptionTypes.CheckNotNull(nameof(exceptionTypes));

            _ignoredExceptions.AddRange(exceptionTypes);
        }

        public TResult Until<TResult>(Func<T, TResult> condition) =>
            throw new NotSupportedException("SafeWaitAsync does not support synchronous Until function, it only supports" +
                " the asynchronous UntilAsync function.");

        /// <summary>
        /// Does the same as <see cref="SafeWait{T}.Until{TResult}(Func{T, TResult})"/> but in async. See documentation
        /// there.
        /// </summary>
        /// <typeparam name="TResult">The delegate's expected return type.</typeparam>
        /// <param name="condition">A delegate taking an object of type T as its parameter, and returning a
        /// TResult.</param>
        /// <returns>The delegate's return value.</returns>
        /// Implements the same logic as <see cref="SafeWait{T}.Until{TResult}(Func{T, TResult})"/>, the complexity is okay.
#pragma warning disable S3776 // Cognitive Complexity of methods should not be too high
        public async Task<TResult> UntilAsync<TResult>(Func<T, Task<TResult>> condition)
#pragma warning restore S3776 // Cognitive Complexity of methods should not be too high
        {
            condition.CheckNotNull(nameof(condition));

            var operationStart = _clock.Now;
            var operationTimeoutEnd = operationStart.Add(Timeout);

            while (true)
            {
                var iterationStart = _clock.Now;

                try
                {
                    var result = await condition(_input);

                    if (DoesConditionResultSatisfy(result))
                        return result;
                }
                catch (Exception exception)
                {
                    if (!IsIgnoredException(exception))
                        throw;
                }

                var iterationEnd = _clock.Now;
                var timeUntilTimeout = operationTimeoutEnd - iterationEnd;

                if (timeUntilTimeout <= TimeSpan.Zero)
                {
                    if (typeof(TResult) == typeof(ReadOnlyCollection<IWebElement>))
                        return (TResult)(object)Array.Empty<IWebElement>().ToReadOnly();

                    return typeof(TResult) == typeof(IWebElement[]) ? (TResult)(object)Array.Empty<IWebElement>() : default;
                }

                var timeToSleep = PollingInterval - (iterationEnd - iterationStart);

                if (timeUntilTimeout < timeToSleep)
                    timeToSleep = timeUntilTimeout;

                if (timeToSleep > TimeSpan.Zero)
                    await Task.Delay(timeToSleep);
            }
        }

        protected virtual bool DoesConditionResultSatisfy<TResult>(TResult result)
        {
            if (typeof(TResult) == typeof(bool))
            {
                var boolResult = result as bool?;

                if (boolResult.HasValue && boolResult.Value)
                    return true;
            }
            else if (!Equals(result, default(TResult)) &&
                (result is not IEnumerable enumerable || enumerable.Cast<object>().Any()))
            {
                return true;
            }

            return false;
        }

        private bool IsIgnoredException(Exception exception) =>
            _ignoredExceptions.Any(type => type.IsAssignableFrom(exception.GetType()));
    }

    internal static class CheckExtensions
    {
        internal static T CheckNotNull<T>(this T value, string argumentName, string errorMessage = null)
        {
            if (Equals(value, default(T)))
                throw new ArgumentNullException(argumentName, errorMessage);

            return value;
        }
    }
}

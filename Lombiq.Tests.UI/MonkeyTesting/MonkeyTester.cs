using Atata;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium.Remote;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Lombiq.Tests.UI.MonkeyTesting
{
    internal sealed class MonkeyTester
    {
        private const string SetIsMonkeyTestRunningScript = "window.isMonkeyTestRunning = true;";

        private const string GetIsMonkeyTestRunningScript = "return !!window.isMonkeyTestRunning;";

        private readonly UITestContext _context;

        private readonly MonkeyTestingOptions _options;

        private readonly Random _random;

        private readonly List<PageMonkeyTestInfo> _pageTestInfoList = new();

        internal MonkeyTester(UITestContext context, MonkeyTestingOptions options = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _options = options ?? new MonkeyTestingOptions();
            _random = new Random(_options.BaseRandomSeed);
        }

        private ILogManager Log => _context.Scope.AtataContext.Log;

        internal void TestOnePage(int? randomSeed = null) =>
            Log.ExecuteSection(
                new LogSection("Execute monkey testing against one page"),
                () =>
                {
                    WriteOptionsToLog();

                    var pageTestInfo = GetCurrentPageTestInfo();

                    if (randomSeed is null) TestCurrentPage(pageTestInfo);
                    else TestCurrentPageWithRandomSeed(pageTestInfo, randomSeed.Value);
                });

        internal void TestRecursively() =>
            Log.ExecuteSection(
                new LogSection($"Execute monkey testing recursively"),
                () =>
                {
                    WriteOptionsToLog();

                    var pageTestInfo = GetCurrentPageTestInfo();
                    TestCurrentPage(pageTestInfo);

                    while (true)
                    {
                        pageTestInfo = GetCurrentPageTestInfo();

                        if (CanTestPage(pageTestInfo))
                        {
                            TestCurrentPage(pageTestInfo);
                        }
                        else if (TryGetLeftPageToTest(out var leftPageToTest))
                        {
                            _context.Scope.AtataContext.Go.ToUrl(leftPageToTest.Url);

                            TestCurrentPage(leftPageToTest);
                        }
                        else
                        {
                            return;
                        }
                    }
                });

        private void WriteOptionsToLog() =>
            Log.Trace(@$"Monkey testing options:
- {nameof(MonkeyTestingOptions.BaseRandomSeed)}={_options.BaseRandomSeed}
- {nameof(MonkeyTestingOptions.PageTestTime)}={_options.PageTestTime.ToShortIntervalString()}
- {nameof(MonkeyTestingOptions.PageMarkerPollingInterval)}={_options.PageMarkerPollingInterval.ToShortIntervalString()}
- {nameof(MonkeyTestingOptions.RunAccessibilityCheckingAssertion)}={_options.RunAccessibilityCheckingAssertion}
- {nameof(MonkeyTestingOptions.RunHtmlValidationAssertion)}={_options.RunHtmlValidationAssertion}
- {nameof(MonkeyTestingOptions.RunAppLogAssertion)}={_options.RunAppLogAssertion}
- {nameof(MonkeyTestingOptions.RunBrowserLogAssertion)}={_options.RunBrowserLogAssertion}
- {nameof(MonkeyTestingOptions.GremlinsSpecies)}={string.Join(',', _options.GremlinsSpecies)}
- {nameof(MonkeyTestingOptions.GremlinsMogwais)}={string.Join(',', _options.GremlinsMogwais)}
- {nameof(MonkeyTestingOptions.GremlinsAttackDelay)}={_options.GremlinsAttackDelay.ToShortIntervalString()}");

        private bool CanTestPage(PageMonkeyTestInfo pageTestInfo)
        {
            bool canTest = pageTestInfo.HasTimeToTest && ShouldTestPageUrl(pageTestInfo.Url);

            if (!canTest)
            {
                Log.Info(
                    !pageTestInfo.HasTimeToTest
                    ? $"\"{pageTestInfo.CleanUrl}\" is tested completely"
                    : $"Navigated to \"{pageTestInfo.Url}\" that should not be tested");
            }

            return canTest;
        }

        private bool ShouldTestPageUrl(string url) =>
            _options.UrlFilters.All(filter => filter.CanHandle(url, _context));

        private bool TryGetLeftPageToTest(out PageMonkeyTestInfo pageTestInfo)
        {
            pageTestInfo = _pageTestInfoList.FirstOrDefault(x => x.HasTimeToTest);
            return pageTestInfo != null;
        }

        private PageMonkeyTestInfo GetCurrentPageTestInfo()
        {
            var url = _context.Driver.Url;
            var cleanUrl = CleanUrl(url);

            var pageTestInfo = _pageTestInfoList.FirstOrDefault(x => x.CleanUrl == cleanUrl)
                ?? new PageMonkeyTestInfo(url, cleanUrl, _options.PageTestTime);

            Log.Info($"Current page is \"{pageTestInfo.CleanUrl}\"");

            return pageTestInfo;
        }

        private string CleanUrl(string url)
        {
            foreach (var cleaner in _options.UrlCleaners)
                url = cleaner.Handle(url, _context);

            return url;
        }

        private void TestCurrentPage(PageMonkeyTestInfo pageTestInfo)
        {
            int randomSeed = GetRandomSeed();

            TestCurrentPageWithRandomSeed(pageTestInfo, randomSeed);
        }

        private void TestCurrentPageWithRandomSeed(PageMonkeyTestInfo pageTestInfo, int randomSeed)
        {
            try
            {
                Log.ExecuteSection(
                    new LogSection(
#pragma warning disable S103 // Lines should not be too long
                       $"Monkey test \"{pageTestInfo.CleanUrl}\" within {pageTestInfo.TimeToTest.ToShortIntervalString()} with {randomSeed} random seed."),
#pragma warning restore S103 // Lines should not be too long
                    () =>
                    {
                        ExecutePreAssertions();

                        var pageTestTimeLeft = TestCurrentPageAndMeasureTestTimeLeft(pageTestInfo.TimeToTest, randomSeed);
                        pageTestInfo.TimeToTest = pageTestTimeLeft;
                        if (!_pageTestInfoList.Contains(pageTestInfo))
                            _pageTestInfoList.Add(pageTestInfo);

                        ExecutePostAssertions();
                    });
            }
            catch (Exception exception)
            {
                throw new AssertionException($"Failure on \"{pageTestInfo.CleanUrl}\" page", exception);
            }
        }

        private void ExecutePreAssertions()
        {
            if (_options.RunAccessibilityCheckingAssertion) _context.AssertAccessibility();

            if (_options.RunHtmlValidationAssertion) _context.AssertHtmlValidityAsync().GetAwaiter().GetResult();
        }

        private void ExecutePostAssertions()
        {
            if (_options.RunAppLogAssertion) _context.AssertAppLogAsync().GetAwaiter().GetResult();

            if (_options.RunBrowserLogAssertion) _context.AssertBrowserLogAsync().GetAwaiter().GetResult();
        }

        [SuppressMessage("Security", "SCS0005:Weak random number generator.", Justification = "For current purpose it should not be secured.")]
        [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "For current purpose it should not be secured.")]
        private int GetRandomSeed() =>
            _random.Next();

        private TimeSpan TestCurrentPageAndMeasureTestTimeLeft(TimeSpan testTime, int randomSeed)
        {
            _context.Driver.ExecuteScript(SetIsMonkeyTestRunningScript);

            string gremlinsScript = BuildGremlinsScript(testTime, randomSeed);
            _context.Driver.ExecuteScript(gremlinsScript);

            return MeasureTimeLeftOfMeetingPredicate(
                _context.Driver,
                driver => !(bool)driver.ExecuteScript(GetIsMonkeyTestRunningScript),
                timeout: testTime,
                pollingInterval: _options.PageMarkerPollingInterval);
        }

        private string BuildGremlinsScript(TimeSpan testTime, int randomSeed) =>
            new GremlinsScriptBuilder
            {
                Species = _options.GremlinsSpecies.ToArray(),
                Mogwais = _options.GremlinsMogwais.ToArray(),
                NumberOfAttacks = (int)(testTime.TotalMilliseconds / _options.GremlinsAttackDelay.TotalMilliseconds),
                AttackDelay = (int)_options.GremlinsAttackDelay.TotalMilliseconds,
                RandomSeed = randomSeed,
            }
            .Build();

        private static TimeSpan MeasureTimeLeftOfMeetingPredicate(
            RemoteWebDriver webDriver,
            Func<RemoteWebDriver, bool> predicate,
            TimeSpan timeout,
            TimeSpan pollingInterval)
        {
            var wait = new SafeWait<RemoteWebDriver>(webDriver)
            {
                Timeout = timeout,
                PollingInterval = pollingInterval,
            };

            var stopwatch = Stopwatch.StartNew();
            var isPageInterrupted = wait.Until(predicate);
            stopwatch.Stop();

            if (isPageInterrupted)
            {
                var timeLeft = timeout - stopwatch.Elapsed;
                return timeLeft > TimeSpan.Zero ? timeLeft : TimeSpan.Zero;
            }
            else
            {
                return TimeSpan.Zero;
            }
        }
    }
}

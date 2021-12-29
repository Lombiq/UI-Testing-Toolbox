using Atata;
using Lombiq.HelpfulLibraries.Libraries.Utilities;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium.Remote;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.MonkeyTesting
{
    internal sealed class MonkeyTester
    {
        private const string SetIsMonkeyTestRunningScript = "window.isMonkeyTestRunning = true;";
        private const string GetIsMonkeyTestRunningScript = "return !!window.isMonkeyTestRunning;";

        private readonly UITestContext _context;
        private readonly MonkeyTestingOptions _options;
        private readonly NonSecurityRandomizer _randomizer;
        private readonly List<PageMonkeyTestInfo> _visitedPages = new();

        private ILogManager Log => _context.Scope.AtataContext.Log;

        internal MonkeyTester(UITestContext context, MonkeyTestingOptions options = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _options = options ?? new MonkeyTestingOptions();
            _randomizer = new NonSecurityRandomizer(_options.BaseRandomSeed);
        }

        internal void TestOnePage(int? randomSeed = null) =>
            Log.ExecuteSection(
                new LogSection("Execute monkey testing against one page"),
                () => TestOnePageAsync(randomSeed).GetAwaiter().GetResult());

        private async Task TestOnePageAsync(int? randomSeed = null)
        {
            WriteOptionsToLog();

            var pageTestInfo = GetCurrentPageTestInfo();

            if (randomSeed is null) await TestCurrentPageAsync(pageTestInfo);
            else await TestCurrentPageWithRandomSeedAsync(pageTestInfo, randomSeed.Value);
        }

        internal void TestRecursively() =>
            Log.ExecuteSection(
                new LogSection($"Execute monkey testing recursively"),
                () => TestRecursivelyAsync().GetAwaiter().GetResult());

        private async Task TestRecursivelyAsync()
        {
            WriteOptionsToLog();

            var pageTestInfo = GetCurrentPageTestInfo();
            await TestCurrentPageAsync(pageTestInfo);

            while (true)
            {
                pageTestInfo = GetCurrentPageTestInfo();

                if (CanTestPage(pageTestInfo))
                {
                    await TestCurrentPageAsync(pageTestInfo);
                }
                else if (TryGetAvailablePageToTest(out var availablePageToTest))
                {
                    _context.GoToAbsoluteUrl(availablePageToTest.Url);

                    await TestCurrentPageAsync(availablePageToTest);
                }
                else
                {
                    return;
                }
            }
        }

        private void WriteOptionsToLog() =>
            Log.Trace(@$"Monkey testing options:
- {nameof(MonkeyTestingOptions.BaseRandomSeed)}={_options.BaseRandomSeed}
- {nameof(MonkeyTestingOptions.PageTestTime)}={_options.PageTestTime.ToShortIntervalString()}
- {nameof(MonkeyTestingOptions.PageMarkerPollingInterval)}={_options.PageMarkerPollingInterval.ToShortIntervalString()}
- {nameof(MonkeyTestingOptions.GremlinsSpecies)}={string.Join(", ", _options.GremlinsSpecies)}
- {nameof(MonkeyTestingOptions.GremlinsMogwais)}={string.Join(", ", _options.GremlinsMogwais)}
- {nameof(MonkeyTestingOptions.GremlinsAttackDelay)}={_options.GremlinsAttackDelay.ToShortIntervalString()}");

        private bool CanTestPage(PageMonkeyTestInfo pageTestInfo)
        {
            bool canTest = pageTestInfo.HasTimeToTest && ShouldTestPageUrl(pageTestInfo.Url);

            if (!canTest)
            {
                Log.Info(
                    !pageTestInfo.HasTimeToTest
                    ? $"Available monkey testing time for \"{pageTestInfo.SanitizedUrl}\" is up and thus testing is complete."
                    : $"Navigated to \"{pageTestInfo.Url}\" that should not be tested.");
            }

            return canTest;
        }

        private bool ShouldTestPageUrl(Uri url) => _options.UrlFilters.All(filter => filter.AllowUrl(_context, url));

        private bool TryGetAvailablePageToTest(out PageMonkeyTestInfo pageTestInfo)
        {
            pageTestInfo = _visitedPages.FirstOrDefault(pageInfo => pageInfo.HasTimeToTest);
            return pageTestInfo != null;
        }

        private PageMonkeyTestInfo GetCurrentPageTestInfo()
        {
            string urlAsString = _context.Driver.Url;
            var url = new Uri(urlAsString);

            var sanitizedUrl = SanitizeUrl(url);

            var pageTestInfo = _visitedPages.FirstOrDefault(pageInfo => pageInfo.SanitizedUrl == sanitizedUrl)
                ?? new PageMonkeyTestInfo(url, sanitizedUrl, _options.PageTestTime);

            Log.Info($"Current page is \"{pageTestInfo.SanitizedUrl}\".");

            return pageTestInfo;
        }

        private Uri SanitizeUrl(Uri url)
        {
            foreach (var sanitizer in _options.UrlSanitizers) url = sanitizer.Sanitize(_context, url);

            return url;
        }

        private Task TestCurrentPageAsync(PageMonkeyTestInfo pageTestInfo)
        {
            int randomSeed = GetRandomSeed();

            return TestCurrentPageWithRandomSeedAsync(pageTestInfo, randomSeed);
        }

        private Task TestCurrentPageWithRandomSeedAsync(PageMonkeyTestInfo pageTestInfo, int randomSeed)
        {
            Log.ExecuteSection(
                new LogSection(
#pragma warning disable S103 // Lines should not be too long
                    $"Monkey test \"{pageTestInfo.SanitizedUrl}\" within {pageTestInfo.TimeToTest.ToShortIntervalString()} with {randomSeed} random seed."),
#pragma warning restore S103 // Lines should not be too long
                () =>
                {
                    var pageTestTimeLeft = TestCurrentPageAndMeasureTestTimeLeft(pageTestInfo.TimeToTest, randomSeed);
                    pageTestInfo.TimeToTest = pageTestTimeLeft;
                    if (!_visitedPages.Contains(pageTestInfo)) _visitedPages.Add(pageTestInfo);
                });

            return _context.TriggerAfterPageChangeEventAsync();
        }

        private int GetRandomSeed() => _randomizer.Get();

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

            return TimeSpan.Zero;
        }
    }
}

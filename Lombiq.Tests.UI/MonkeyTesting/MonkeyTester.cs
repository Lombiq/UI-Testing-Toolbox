using Atata;
using Lombiq.HelpfulLibraries.Libraries.Utilities;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
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

        internal async Task TestOnePageAsync(int? randomSeed = null)
        {
            Log.Start(new LogSection("Execute monkey testing against one page"));

            try
            {
                WriteOptionsToLog();

                var pageTestInfo = GetCurrentPageTestInfo();

                if (randomSeed is null) await TestCurrentPageAsync(pageTestInfo);
                else await TestCurrentPageWithRandomSeedAsync(pageTestInfo, randomSeed.Value);
            }
            finally
            {
                Log.EndSection();
            }
        }

        internal async Task TestRecursivelyAsync()
        {
            Log.Start(new LogSection($"Execute monkey testing recursively"));

            try
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
                        await _context.GoToAbsoluteUrlAsync(availablePageToTest.Url);

                        await TestCurrentPageAsync(availablePageToTest);
                    }
                    else
                    {
                        return;
                    }
                }
            }
            finally
            {
                Log.EndSection();
            }
        }

        private void WriteOptionsToLog() =>
            Log.Trace(@$"Monkey testing options:
- {nameof(MonkeyTestingOptions.BaseRandomSeed)}={_options.BaseRandomSeed.ToTechnicalString()}
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
                    $"Monkey test \"{pageTestInfo.SanitizedUrl}\" within {pageTestInfo.TimeToTest.ToShortIntervalString()} " +
                    $"with {randomSeed.ToTechnicalString()} random seed."),
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
            // If Gremlin interactions cause the new tabs/windows to open, we need to switch back to the original one.
            _context.SwitchToCurrentWindow();

            _context.Driver.ExecuteScript(GremlinsScripts.GremlinsScript);

            string gremlinsRunScript = BuildGremlinsRunScript(testTime, randomSeed);
            _context.Driver.ExecuteScript(gremlinsRunScript);

            var testTimeLeft = MeasureTimeLeftOfMeetingPredicate(
                _context.Driver,
                driver => !(bool)driver.ExecuteScript(GremlinsScripts.GetAreGremlinsRunningScript),
                timeout: testTime,
                pollingInterval: _options.PageMarkerPollingInterval);

            _context.SwitchToCurrentWindow();

            _context.Driver.ExecuteScript(GremlinsScripts.StopGremlinsScript);

            WaitForGremlinsIndicatorsToDisappear();

            var lastGremlinsClickLogMessage = (string)_context.Driver.ExecuteScript(GremlinsScripts.GetLastGremlinsClickLogMessageScript);

            if (!string.IsNullOrEmpty(lastGremlinsClickLogMessage))
                Log.Info($"Last Gremlins click: {lastGremlinsClickLogMessage}.");

            return testTimeLeft;
        }

        private void WaitForGremlinsIndicatorsToDisappear() =>
            _context.Driver.Missing(
                By.CssSelector("body>div[style^='z-index: 2000;']")
                    .Within(TimeSpan.FromSeconds(10))
                    .OfAnyVisibility());

        private string BuildGremlinsRunScript(TimeSpan testTime, int randomSeed) =>
            new GremlinsScripts.RunScriptBuilder
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
            wait.Until(predicate);
            stopwatch.Stop();

            var timeLeft = timeout - stopwatch.Elapsed;
            return timeLeft > TimeSpan.Zero ? timeLeft : TimeSpan.Zero;
        }
    }
}

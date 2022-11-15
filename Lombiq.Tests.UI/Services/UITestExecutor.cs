using Lombiq.HelpfulLibraries.Common.Utilities;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services.GitHub;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Services;

public static class UITestExecutor
{
    private static readonly object _numberOfTestsLimitLock = new();
    private static SemaphoreSlim _numberOfTestsLimit;

    /// <summary>
    /// Executes a test on a new Orchard Core web app instance within a newly created Atata scope.
    /// </summary>
    public static Task ExecuteOrchardCoreTestAsync<TEntryPoint>(
        UITestManifest testManifest,
        OrchardCoreUITestExecutorConfiguration configuration)
        where TEntryPoint : class
    {
        if (string.IsNullOrEmpty(testManifest.Name))
        {
            throw new ArgumentException("You need to specify the name of the test.");
        }

        if (configuration.OrchardCoreConfiguration == null)
        {
            throw new ArgumentException($"{nameof(configuration.OrchardCoreConfiguration)} should be provided.");
        }

        configuration.TestOutputHelper.WriteLine(
            "NOTE: This log is cumulative for all test execution attempts. If the test fails repeatedly with " +
            "retries then Attempt 0's output will contain only that execution's output, but Attempt 2's will " +
            "contain 0's and 1's too in addition to 2's.");
        configuration.TestOutputHelper.WriteLineTimestampedAndDebug("Starting preparation for {0}.", testManifest.Name);

        configuration.AtataConfiguration.TestName = testManifest.Name;

        var dumpRootPath = PrepareDumpFolder(testManifest, configuration);

        if (configuration.AccessibilityCheckingConfiguration.CreateReportAlways)
        {
            var directoryPath = configuration.AccessibilityCheckingConfiguration.AlwaysCreatedAccessibilityReportsDirectoryPath;
            FileSystemHelper.EnsureDirectoryExists(directoryPath);
        }

        configuration.TestOutputHelper.WriteLineTimestampedAndDebug("Finished preparation for {0}.", testManifest.Name);

        if (_numberOfTestsLimit == null && configuration.MaxParallelTests > 0)
        {
            lock (_numberOfTestsLimitLock)
            {
                _numberOfTestsLimit ??= new SemaphoreSlim(configuration.MaxParallelTests);
            }
        }

        return ExecuteOrchardCoreTestInnerAsync<TEntryPoint>(testManifest, configuration, dumpRootPath);
    }

    private static async Task ExecuteOrchardCoreTestInnerAsync<TEntryPoint>(
        UITestManifest testManifest,
        OrchardCoreUITestExecutorConfiguration configuration,
        string dumpRootPath)
        where TEntryPoint : class
    {
        var retryCount = 0;
        var passed = false;
        while (!passed)
        {
            try
            {
                if (_numberOfTestsLimit != null)
                {
                    await _numberOfTestsLimit.WaitAsync();
                }

                await using var instance = new UITestExecutionSession<TEntryPoint>(testManifest, configuration);
                passed = await instance.ExecuteAsync(retryCount, dumpRootPath);
            }
            catch (Exception ex) when (retryCount < configuration.MaxRetryCount)
            {
                configuration.TestOutputHelper.WriteLineTimestampedAndDebug(
                    $"Unhandled exception during text execution: {ex}.");
            }
            catch (Exception ex)
            {
                // When the last try failed.

                if (GitHubActionsGroupingTestOutputHelper.IsGitHubEnvironment.Value)
                {
                    new GitHubAnnotationWriter(configuration.TestOutputHelper)
                        .ErrorInTest(ex, testManifest.XunitTest.TestCase);
                }

                throw;
            }
            finally
            {
                if (configuration.ReportTeamCityMetadata && (passed || retryCount == configuration.MaxRetryCount))
                {
                    TeamCityMetadataReporter.ReportInt(testManifest, "TryCount", retryCount + 1);
                }

                _numberOfTestsLimit?.Release();
            }

            retryCount++;
        }
    }

    private static string PrepareDumpFolder(
        UITestManifest testManifest,
        OrchardCoreUITestExecutorConfiguration configuration)
    {
        var dumpConfiguration = configuration.FailureDumpConfiguration;
        var dumpFolderNameBase = testManifest.Name;
        if (dumpConfiguration.UseShortNames && dumpFolderNameBase.Contains('(', StringComparison.Ordinal))
        {
            var dumpFolderNameBeginningIndex =
                dumpFolderNameBase[..dumpFolderNameBase.IndexOf('(', StringComparison.Ordinal)].LastIndexOf('.') + 1;
            dumpFolderNameBase = dumpFolderNameBase[dumpFolderNameBeginningIndex..];
        }

        dumpFolderNameBase = dumpFolderNameBase.MakeFileSystemFriendly();

        var dumpRootPath = Path.Combine(dumpConfiguration.DumpsDirectoryPath, dumpFolderNameBase);

        DirectoryHelper.SafelyDeleteDirectoryIfExists(dumpRootPath);

        // Probe creating the directory. At least on Windows this can still fail with "The filename, directory name, or
        // volume label syntax is incorrect" but not simply due to the presence of specific characters. Maybe both
        // length and characters play a role (a path containing either the same characters or having the same length
        // would work but not both). Playing safe here.

        try
        {
            Directory.CreateDirectory(dumpRootPath);
            DirectoryHelper.SafelyDeleteDirectoryIfExists(dumpRootPath);
        }
        catch (Exception ex) when (
            (ex is IOException &&
                ex.Message.ContainsOrdinalIgnoreCase("The filename, directory name, or volume label syntax is incorrect."))
            || ex is PathTooLongException)
        {
            // The OS doesn't like the path or it's too long. So we shorten it by removing the test parameters which
            // usually make it long.

            var openingBracketIndex = dumpFolderNameBase.IndexOf('(', StringComparison.Ordinal);
            var closingBracketIndex = dumpFolderNameBase.LastIndexOf(")", StringComparison.Ordinal);

            // Can't use string.GetHasCode() because that varies between executions.
            var hashedParameters = Sha256Helper
                .ComputeHash(dumpFolderNameBase[(openingBracketIndex + 1)..(closingBracketIndex + 1)]);

            dumpFolderNameBase =
                dumpFolderNameBase[0..(openingBracketIndex + 1)] +
                hashedParameters +
                dumpFolderNameBase[closingBracketIndex..];

            dumpRootPath = Path.Combine(dumpConfiguration.DumpsDirectoryPath, dumpFolderNameBase);

            DirectoryHelper.SafelyDeleteDirectoryIfExists(dumpRootPath);

            configuration.TestOutputHelper.WriteLineTimestampedAndDebug(
                "Couldn't create a folder with the same name as the test. A TestName.txt file containing the " +
                    "full name ({0}) will be put into the folder to help troubleshooting if the test fails.",
                testManifest.Name);
        }

        return dumpRootPath;
    }
}

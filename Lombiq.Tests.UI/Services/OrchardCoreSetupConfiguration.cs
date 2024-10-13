using Lombiq.Tests.UI.Constants;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services;

public delegate Task BeforeSetupHandler(OrchardCoreUITestExecutorConfiguration configuration);
public delegate Task AfterSetupHandler(OrchardCoreUITestExecutorConfiguration configuration);

/// <summary>
/// Configuration for the initial setup of an Orchard Core app.
/// </summary>
public class OrchardCoreSetupConfiguration
{
    /// <summary>
    /// Gets or sets the Orchard setup operation so the result can be snapshot and used in subsequent tests.
    /// WARNING: It's highly recommended to put assertions at the end to detect setup issues and fail tests early (since
    /// they can't be reliable after an improper setup). If configured, automatic log assertions will be executed after
    /// the setup operation too. Also see <see cref="FastFailSetup"/>.
    /// </summary>
    public Func<UITestContext, Task<Uri>> SetupOperation { get; set; }

    /// <summary>
    /// Gets or sets a function that calculates a unique ID which represents the setup operation. This is used when
    /// generating the setup snapshot path. If you set the <see cref="SetupOperation"/> to a dynamic value (e.g. using a
    /// lambda expression or a method that returns a new <see cref="Func{T,TResult}"/> instance) then you must set this
    /// property to a custom function. Otherwise, it's safe to leave it as-is.
    /// </summary>
    public Func<Func<UITestContext, Task<Uri>>, string> SetupOperationIdentifierCalculator { get; set; } =
        setupOperation => setupOperation.GetHashCode().ToTechnicalString();

    /// <summary>
    /// Gets or sets a value indicating whether if a specific setup operations fails and exhausts <see
    /// cref="OrchardCoreUITestExecutorConfiguration.MaxRetryCount"/> globally then all tests using the same operation
    /// should fail immediately, without attempting to run it again. If set to <see langword="false"/> then every test
    /// will retry the setup until each tests' <see cref="OrchardCoreUITestExecutorConfiguration.MaxRetryCount"/>
    /// allows. If set to <see langword="true"/> then the setup operation will only be retried <see
    /// cref="OrchardCoreUITestExecutorConfiguration.MaxRetryCount"/> times (as set by the first test running that
    /// operation) altogether. Defaults to <see langword="true"/>.
    /// </summary>
    public bool FastFailSetup { get; set; } = true;

    public string SetupSnapshotDirectoryPath { get; set; } =
        Path.Combine(DirectoryPaths.Temp, DirectoryPaths.SetupSnapshot);

    public BeforeSetupHandler BeforeSetup { get; set; }
    public AfterSetupHandler AfterSetup { get; set; }

    /// <summary>
    /// If <see cref="SetupOperation"/> is not <see langword="null"/>, it invokes <see
    /// cref="SetupOperationIdentifierCalculator"/> with the <see cref="SetupOperation"/> and returns the result.
    /// </summary>
    public string CalculateSetupOperationIdentifier() =>
        SetupOperation is null ? null : SetupOperationIdentifierCalculator(SetupOperation);
}

using Deque.AxeCore.Commons;
using System;
using System.Collections.Generic;

namespace Lombiq.Tests.UI.Models;

public class AccessibilityCheckingResult
{
    public IList<AxeResultItem> Violations { get; init; } = [];
    public IList<AxeResultItem> Passes { get; init; } = [];
    public IList<AxeResultItem> Inapplicable { get; init; } = [];
    public IList<AxeResultItem> Incomplete { get; init; } = [];
    public DateTimeOffset? Timestamp { get; set; }
    public AxeTestEnvironment TestEnvironment { get; set; }
    public AxeTestRunner TestRunner { get; set; }
    public string Url { get; set; }
    public AxeTestEngine TestEngine { get; set; }
    public object ToolOptions { get; set; }

    public static implicit operator AccessibilityCheckingResult(AxeResult axeResult) =>
        new()
        {
            Violations = [.. axeResult.Violations],
            Passes = [.. axeResult.Passes],
            Inapplicable = [.. axeResult.Inapplicable],
            Incomplete = [.. axeResult.Incomplete],
            Timestamp = axeResult.Timestamp,
            TestEnvironment = axeResult.TestEnvironment,
            TestRunner = axeResult.TestRunner,
            Url = axeResult.Url,
            TestEngine = axeResult.TestEngine,
            ToolOptions = axeResult.ToolOptions,
        };
}

#nullable enable

using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;

namespace Lombiq.Tests.UI.Models;

public record ClickReliablyErrorContext(
    UITestContext Context,
    IWebElement Element,
    By? OriginalSelector,
    Exception Exception,
    bool ShouldThrow = false);

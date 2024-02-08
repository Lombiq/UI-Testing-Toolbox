using Lombiq.Tests.UI.Extensions;
using OpenQA.Selenium;
using Shouldly;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests;

public class RemoteTests : RemoteUITestBase
{
    public RemoteTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public Task ExampleDotComShouldWork() =>
        ExecuteTestAsync(
            new Uri("https://example.com/"),
            context =>
            {
                context.Get(By.CssSelector("h1")).Text.ShouldBe("Example Domain");
                context.Exists(By.LinkText("More information..."));
            });
}

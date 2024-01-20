using Atata;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Services;

public class TestOutputLogConsumer(ITestOutputHelper testOutputHelper) : TextOutputLogConsumer
{
    protected override void Write(string completeMessage) => testOutputHelper.WriteLine(completeMessage);
}

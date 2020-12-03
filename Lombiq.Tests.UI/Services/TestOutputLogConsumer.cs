using Atata;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Services
{
    public class TestOutputLogConsumer : TextOutputLogConsumer
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public TestOutputLogConsumer(ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;

        protected override void Write(string completeMessage) => _testOutputHelper.WriteLine(completeMessage);
    }
}

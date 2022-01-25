using Lombiq.Tests.UI.Services;
using System;
using System.Globalization;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests
{
    // We'll see some simpler tests as a start. Each of them will teach us important concepts.
    public class BasicTests : UITestBase
    {
        public BasicTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Theory]
        [InlineData(Browser.Chrome)]
        public void LoginShouldWork(Browser browser) =>
            SendMetadata($"Lombiq.Tests.UI.Samples.Tests.BasicTests.LoginShouldWork(browser: {browser})", "Abc", 123.45);

        private void SendMetadata(string testName, string name, double value) =>
#pragma warning disable S103 // Lines should not be too long
            _testOutputHelper.WriteLine($"##teamcity[testMetadata testName='{Encode(testName)}' name='{Encode(name)}' type='number' value='{value.ToString(CultureInfo.InvariantCulture)}']");
#pragma warning restore S103 // Lines should not be too long

        private static string Encode(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            var sb = new StringBuilder(value.Length * 2);
            foreach (var ch in value)
                switch (ch)
                {
                    case '|':
                        sb.Append("||");
                        break;
                    case '\'':
                        sb.Append("|'");
                        break;
                    case '\n':
                        sb.Append("|n");
                        break;
                    case '\r':
                        sb.Append("|r");
                        break;
                    case '[':
                        sb.Append("|[");
                        break;
                    case ']':
                        sb.Append("|]");
                        break;
                    case '\u0085':
                        sb.Append("|x");
                        break; //\u0085 (next line)=>|x
                    case '\u2028':
                        sb.Append("|l");
                        break; //\u2028 (line separator)=>|l
                    case '\u2029':
                        sb.Append("|p");
                        break;
                    default:
                        if (ch > 127)
                        {
                            sb.Append($"|0x{(ulong)ch:x4}");
                        }
                        else
                        {
                            sb.Append(ch);
                        }

                        break;
                }

            return sb.ToString();
        }
    }
}

// END OF TRAINING SECTION: UI Testing Toolbox basics.
// NEXT STATION: Head over to Tests/BasicOrchardFeaturesTests.cs.

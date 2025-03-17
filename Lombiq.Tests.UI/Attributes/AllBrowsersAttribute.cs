using Lombiq.Tests.UI.Services;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace Lombiq.Tests.UI.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class AllBrowsersAttribute : DataAttribute
{
    public override ValueTask<IReadOnlyCollection<ITheoryDataRow>> GetData(MethodInfo testMethod, DisposalTracker disposalTracker)
    {
        var browsers = (IEnumerable<Browser>)Enum.GetValues(typeof(Browser));
        var dataRows = new List<ITheoryDataRow>();

        foreach (var browser in browsers)
        {
            dataRows.Add(new TheoryDataRow(browser));
        }

        return new ValueTask<IReadOnlyCollection<ITheoryDataRow>>(dataRows.AsReadOnly());
    }

    public override bool SupportsDiscoveryEnumeration() => true;
}

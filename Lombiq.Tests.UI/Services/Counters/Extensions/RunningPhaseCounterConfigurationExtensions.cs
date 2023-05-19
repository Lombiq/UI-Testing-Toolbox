using Lombiq.HelpfulLibraries.OrchardCore.Mvc;
using Lombiq.Tests.UI.Services.Counters.Configuration;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services.Counters.Extensions;

public static class RunningPhaseCounterConfigurationExtensions
{
    public static CounterConfiguration AddIfMissingAndConfigure(
        this RunningPhaseCounterConfiguration configuration,
        ICounterConfigurationKey key,
        Action<CounterConfiguration> configure)
    {
        var phaseConfiguration = configuration.GetMaybeByKey(key);
        if (phaseConfiguration is not null)
        {
            phaseConfiguration = configuration[key];
            configure?.Invoke(phaseConfiguration);

            return phaseConfiguration;
        }

        phaseConfiguration = new PhaseCounterConfiguration();
        configure.Invoke(phaseConfiguration);
        configuration.Add(key, phaseConfiguration);

        return phaseConfiguration;
    }

    public static void ConfigureForRelativeUrl<TController>(
        this RunningPhaseCounterConfiguration configuration,
        Action<CounterConfiguration> configure,
        Expression<Func<TController, Task>> actionExpressionAsync,
        bool exactMatch,
        params (string Key, object Value)[] additionalArguments)
        where TController : ControllerBase =>
        configuration.AddIfMissingAndConfigure(
            new RelativeUrlConfigurationKey(
                new Uri(
                    TypedRoute.CreateFromExpression(actionExpressionAsync.StripResult(), additionalArguments).ToString(),
                    UriKind.Relative),
                exactMatch),
            configure);

    public static CounterConfiguration GetMaybeByKey(
        this RunningPhaseCounterConfiguration configuration,
        ICounterConfigurationKey key)
    {
        var configurationKey = configuration.Keys.FirstOrDefault(item => item.Equals(key));
        if (configurationKey is null)
        {
            return null;
        }

        return configuration[configurationKey];
    }
}

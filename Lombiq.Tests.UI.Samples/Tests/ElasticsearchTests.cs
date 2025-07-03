using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Samples.Helpers;
using OpenQA.Selenium;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Lombiq.Tests.UI.Samples.Tests;

// You can use Elasticsearch for keyword search and querying in Orchard Core. And you can also test it with the UI
// Testing Toolbox! Everything that makes this hard, like indexing happening in the background and parallel tests
// wanting to use the same Elasticsearch deployment are handled automatically.

// This test demonstrates a simple check of the built-in Orchard Core search feature.

// If you use the Build and Test Orchard Core workflow of Lombiq GitHub Actions for CI builds (see
// https://github.com/Lombiq/GitHub-Actions/blob/dev/Docs/Workflows/BuildDotNetCoreOrchardCore/BuildAndTestOrchardCoreSolution.md),
// then you can also utilize Elasticsearch in CI test runs, without needing to do any setup on your own.
public class ElasticsearchTests : UITestBase
{
    public ElasticsearchTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public Task ElasticsearchShouldWork() =>
        ExecuteTestAsync(
            async context =>
            {
                // Going to the built-in search feature. By default, it's not accessible for anonymous users, so we need
                // to log in.
                await context.SignInDirectlyAndGoToRelativeUrlAsync("/search");

                // Filling out the search form, looking for the sample blog post coming from the built-in Blog recipe.
                await context.ClickAndFillInWithRetriesAsync(By.Name("Terms"), "exploration");
                await context.ClickReliablyOnAsync(By.XPath("//button[@class='btn btn-primary btn-sm']"));

                // Hopefully, we found it!
                context.Exists(By.XPath("//h2[contains(., 'Man must explore, and this is exploration at its greatest')]"));
            },
            // Since indexing needs to happen before we can start the test, we use a custom setup recipe that configures
            // Elasticsearch. The recipe is here in the test project, not the web app; this is also possible, and what
            // we recommend for recipes that you only need for testing.
            context => SetupHelpers.RunSetupAsync(context, "Lombiq.Tests.UI.Samples.Elasticsearch"),
            configuration =>
            {
                // This is important. Here we tell the UI Testing Toolbox that we want to use Elasticsearch. You can set
                // this for just a given test, as we do it here, or for all tests from a base class like UITestBase that
                // this class inherits from.
                configuration.UseElasticsearch = true;

                // The search page has some HTML validation issues, but we don't care about those here.
                configuration.HtmlValidationConfiguration.RunHtmlValidationAssertionOnAllPageChanges = false;
            });
}

// END OF TRAINING SECTION: Testing Elasticsearch-using functionality.
// NEXT STATION: Head over to FrontendUITestBase.cs.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Lombiq.Tests.UI.Helpers;

public static class TeamsHelper
{
    public static Task<bool> SendFailedUiTestTeamsMessageAsync(string webhookUrl)
    {
        // This needs a workflow in Teams. Add one starting with the "When a Teams webhook request is received"
        // trigger ("Who can trigger the flow?" -> Anyone). Then add the "Post card in a chat or channel" step with
        // `@triggerBody()` (the @ is important) as the "Adaptive Card" value. Just paste this into the text box,
        // trying to insert it as a dynamic content or expression value won't work.
        var runUrl =
            $"https://github.com/{Environment.GetEnvironmentVariable("GITHUB_REPOSITORY")}/actions/runs/" +
            $"{Environment.GetEnvironmentVariable("GITHUB_RUN_ID")}";

        var message = "At least one UI test among the ones that test DotNest Tenants Core Production failed. " +
            "This can indicate the production app being down, or a recent change breaking a " +
            "feature in production. Please investigate ASAP.";

        return SendTeamsMessageAsync(webhookUrl, "Testing Production failed", message, "Go to workflow run", runUrl);
    }

    public static async Task<bool> SendTeamsMessageAsync(string webhookUrl, string title, string message, string urlText, string url)
    {
        var adaptiveCardContent = new Dictionary<string, object>
        {
            { "type", "AdaptiveCard" },
            {
                "body", new object[]
                {
                    new Dictionary<string, object>
                    {
                        { "type", "TextBlock" },
                        { "text", title },
                        { "weight", "Bolder" },
                        { "size", "ExtraLarge" },
                    },
                    new Dictionary<string, object>
                    {
                        { "type", "TextBlock" },
                        {
                            "text",
                            message
                        },
                        { "wrap", true },
                    },
                }
            },
            {
                "actions", new object[]
                {
                    new Dictionary<string, object>
                    {
                        { "type", "Action.OpenUrl" },
                        { "title", urlText },
                        { "url", url },
                    },
                }
            },
            { "$schema", "http://adaptivecards.io/schemas/adaptive-card.json" },
            { "version", "1.2" },
        };

        using var client = new HttpClient();

        var adaptiveCardJson = JsonSerializer.Serialize(adaptiveCardContent);
        using var data = new StringContent(adaptiveCardJson, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(webhookUrl, data, TestContext.Current.CancellationToken);

        return response.IsSuccessStatusCode;
    }
}

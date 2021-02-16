using Lombiq.HelpfulLibraries.Libraries.Mvc;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Shortcuts.Controllers
{
    [DevelopmentAndLocalhostOnly]
    public class AzureCacheController : Controller
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public async Task<IActionResult> PurgeAzureCacheDirectly()
        {
            const string jsonBody = @"{""type"":""browser""}";

            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            var baseUri = new Uri($"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/");
            using var response = await _httpClient.PostAsync(new Uri(baseUri, "/Admin/MediaCache/Purge"), content);
            var responseBody = await response.Content.ReadAsStringAsync();

            return Ok();
        }
    }
}

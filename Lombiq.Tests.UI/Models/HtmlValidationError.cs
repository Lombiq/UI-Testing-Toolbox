using Azure.Core.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lombiq.Tests.UI.Models;

public class HtmlValidationError
{
    [JsonPropertyName("ruleId")]
    public string RuleId { get; set; }
    [JsonPropertyName("severity")]
    public int Severity { get; set; }
    [JsonPropertyName("message")]
    public string Message { get; set; }
    [JsonPropertyName("offset")]
    public int Offset { get; set; }
    [JsonPropertyName("line")]
    public int Line { get; set; }
    [JsonPropertyName("column")]
    public int Column { get; set; }
    [JsonPropertyName("size")]
    public int Size { get; set; }
    [JsonPropertyName("selector")]
    public string Selector { get; set; }
    [JsonPropertyName("ruleUrl")]
    public string RuleUrl { get; set; }
    [JsonPropertyName("context")]
    public JsonElement Context { get; set; }
}

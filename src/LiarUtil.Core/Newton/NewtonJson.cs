using System.Text.Json;
using System.Text.Json.Nodes;

namespace LiarUtil.Core.Newton;

internal static class NewtonJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
    };

    public static JsonNode Parse(string json) =>
        JsonNode.Parse(json, nodeOptions: null, documentOptions: new JsonDocumentOptions { AllowTrailingCommas = true })
        ?? throw new NewtonException(LiarUtil.Core.Strings.JSONRootNodeInvalid);

    public static JsonObject ParseObject(string json) =>
        Parse(json) as JsonObject ?? throw new NewtonException(LiarUtil.Core.Strings.JSONRootNodeMustObject);

    public static string Serialize(JsonNode node) => node.ToJsonString(Options);
}

using BaristaApp.Web;
using ModelContextProtocol.Extensions.Apps;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BaristaApp.Resources;

/// <summary>
/// MCP Apps UI resource for the Barista App.
/// The ui:// scheme and the <c>text/html;profile=mcp-app</c> MIME type are what an MCP Apps host
/// looks for when a tool carrying <c>_meta.ui.resourceUri</c> is called.
/// </summary>
[McpServerResourceType]
public static class BaristaResources
{
    /// <summary>URI of the barista UI resource, referenced by <c>[McpAppUi]</c> on the tools.</summary>
    public const string AppUri = "ui://barista/app.html";

    /// <summary>UI metadata advertised in <c>resources/list</c> (via <c>[McpMeta]</c>) and in <c>resources/read</c> (via <see cref="UiMeta"/>).</summary>
    private const string UiMetaJson = """{"prefersBorder":true}""";

    [McpServerResource(
        UriTemplate = AppUri,
        Name = "barista-app",
        MimeType = McpApps.HtmlMimeType,
        Title = "Barista App")]
    [McpMeta("ui", JsonValue = UiMetaJson)]
    [Description("Interactive De'Longhi barista web interface")]
    public static TextResourceContents GetBaristaApp() => new()
    {
        Uri = AppUri,
        MimeType = McpApps.HtmlMimeType,
        Text = BeverageHtml.Page,
        // [McpMeta] only decorates the resource *listing*; MCP Apps hosts read csp / prefersBorder
        // from contents[0]._meta.ui of the *read* response, so the metadata is set here as well.
        Meta = new JsonObject { ["ui"] = UiMeta() },
    };

    private static JsonNode? UiMeta() =>
        JsonSerializer.SerializeToNode(new McpUiResourceMeta { PrefersBorder = true }, McpApps.SerializerOptions);
}

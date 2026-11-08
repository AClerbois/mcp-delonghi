using BaristaApp.Web;
using ModelContextProtocol.Extensions.Apps;
using ModelContextProtocol.Server;
using System.ComponentModel;

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

    [McpServerResource(
        UriTemplate = AppUri,
        Name = "barista-app",
        MimeType = McpApps.HtmlMimeType,
        Title = "Barista App")]
    [McpMeta("ui", JsonValue = """{"prefersBorder":true}""")]
    [Description("Interactive De'Longhi barista web interface")]
    public static Task<string> GetBaristaApp() => Task.FromResult(BeverageHtml.Page);
}

using BaristaApp.Web;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace BaristaApp.Resources;

[McpServerResourceType]
public static class BaristaResources
{
    [McpServerResource(
        UriTemplate = "ui://barista/app.html",
        MimeType = "text/html",
        Title = "Barista App")]
    [Description("Interactive De'Longhi barista web interface")]
    public static Task<string> GetBaristaApp() => Task.FromResult(BeverageHtml.Page);
}

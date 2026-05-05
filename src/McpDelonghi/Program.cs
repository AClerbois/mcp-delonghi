using McpDelonghi.Api;
using McpDelonghi.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Log only to stderr so stdout stays clean for MCP stdio transport
builder.Logging.ClearProviders();
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Core services — headers required by Ayla Networks (406 without Accept, some endpoints check User-Agent)
builder.Services.AddSingleton(_ =>
{
    var client = new HttpClient();
    client.DefaultRequestHeaders.UserAgent.ParseAdd("DeLonghiCoffeeLink/5.0.1 (Android)");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    return client;
});
builder.Services.AddSingleton<DelonghiAuthService>();
builder.Services.AddSingleton<AylaApiClient>();

// MCP server: stdio transport + discover all [McpServerTool] methods in this assembly
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly(typeof(Program).Assembly);

await builder.Build().RunAsync();

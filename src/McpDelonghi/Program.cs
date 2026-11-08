using McpDelonghi.Api;
using McpDelonghi.Auth;
using McpDelonghi.Web;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;

var builder = WebApplication.CreateBuilder(args);

// Suppress ASP.NET Core startup messages so stdout stays clean for MCP stdio transport
builder.WebHost.SuppressStatusMessages(true);
builder.WebHost.UseUrls(
    Environment.GetEnvironmentVariable("DELONGHI_WEB_URL") ?? "http://localhost:5100");

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

// MCP server: stdio transport + discover all [McpServerTool] methods in this assembly.
// SDK 2.x speaks the MCP 2026-07-28 revision and falls back to 2025-11-25 clients automatically.
builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new Implementation
        {
            Name = "mcp-delonghi",
            Title = "De'Longhi Coffee Link",
            Version = typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "1.0.0",
            WebsiteUrl = "https://github.com/aclerbois/mcp-delonghi",
        };

        // Sent to the client at connection time and typically injected as a system message,
        // so every MCP host (VS Code, Claude, Copilot Studio…) gets the same usage guidance.
        options.ServerInstructions = """
            This server controls a single De'Longhi coffee machine through the Coffee Link cloud.
            Before brewing: call get_machine_status and only proceed when ready_to_brew is true;
            if the machine is off, call power_on and wait 30-60 seconds for it to heat up.
            Use get_beverages to discover valid beverage keys before calling brew_beverage.
            Never brew more than one beverage per user request unless explicitly asked.
            stop_beverage discards the drink in progress — use it only when the user asks to cancel.
            The De'Longhi Coffee Link mobile app must be closed while this server is in use.
            """;
    })
    .WithStdioServerTransport()
    .WithToolsFromAssembly(typeof(Program).Assembly);

var app = builder.Build();

// ── Barista Web App ──────────────────────────────────────────────────────────
app.MapGet("/", () => Results.Content(BeverageHtml.Page, "text/html; charset=utf-8"));

app.MapGet("/api/beverages", async (AylaApiClient api, CancellationToken ct) =>
{
    var beverages = await api.GetAvailableBeveragesAsync(ct);
    return Results.Ok(beverages.Select(b => new { key = b.Key, name = b.Name }));
});

app.MapPost("/api/brew", async (BrewRequest req, AylaApiClient api, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(req.BeverageKey))
        return Results.BadRequest(new { error = "BeverageKey is required." });
    try
    {
        await api.BrewBeverageAsync(req.BeverageKey, req.Profile, req.QuantityMl, ct);
        return Results.Ok(new { message = $"Preparation de '{req.BeverageKey}' (profil {req.Profile})... Buon caffe !" });
    }
    catch (DelonghiApiException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

await app.RunAsync();

record BrewRequest(string BeverageKey, int Profile = 2, int? QuantityMl = null);

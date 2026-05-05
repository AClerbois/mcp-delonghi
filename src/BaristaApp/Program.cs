using BaristaApp.Api;
using BaristaApp.Auth;
using BaristaApp.Web;
using Microsoft.Extensions.Logging;

// ── Configuration ─────────────────────────────────────────────────────────────
var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls(
    Environment.GetEnvironmentVariable("BARISTA_APP_URL") ?? "http://localhost:5200");

// Log only to stderr so stdout stays clean when running as an MCP stdio server
builder.Logging.ClearProviders();
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Logging.SetMinimumLevel(LogLevel.Information);

// ── HTTP client — headers required by Ayla Networks ──────────────────────────
builder.Services.AddSingleton(_ =>
{
    var client = new HttpClient();
    client.DefaultRequestHeaders.UserAgent.ParseAdd("DeLonghiCoffeeLink/5.0.1 (Android)");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    return client;
});

builder.Services.AddSingleton<DelonghiAuthService>();
builder.Services.AddSingleton<AylaApiClient>();

// ── MCP server ────────────────────────────────────────────────────────────────
// HTTP Streamable transport (POST /mcp) — used by the VS Code MCP Apps host.
// The tools in BaristaApp.Tools are discovered automatically via [McpServerToolType].
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly(typeof(Program).Assembly)
    .WithResourcesFromAssembly(typeof(Program).Assembly);

var app = builder.Build();

// ── MCP endpoint (Streamable HTTP) ───────────────────────────────────────────
app.MapMcp("/mcp");

// ── Barista Web App ───────────────────────────────────────────────────────────
// GET /  → barista HTML UI (the MCP App rendered in the host's webview)
app.MapGet("/", () => Results.Content(BeverageHtml.Page, "text/html; charset=utf-8"));

// GET /api/beverages → list of beverages (called by the HTML UI)
app.MapGet("/api/beverages", async (AylaApiClient api, CancellationToken ct) =>
{
    var beverages = await api.GetAvailableBeveragesAsync(ct);
    return Results.Ok(beverages.Select(b => new { key = b.Key, name = b.Name }));
});

// POST /api/brew → brew a beverage (called by the HTML UI)
app.MapPost("/api/brew", async (BrewRequest req, AylaApiClient api, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(req.BeverageKey))
        return Results.BadRequest(new { error = "BeverageKey is required." });
    try
    {
        await api.BrewBeverageAsync(req.BeverageKey, req.Profile, req.QuantityMl, ct);
        return Results.Ok(new { message = $"Préparation de '{req.BeverageKey}' (profil {req.Profile})… Buon caffè !" });
    }
    catch (DelonghiApiException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

await app.RunAsync();

record BrewRequest(string BeverageKey, int Profile = 2, int? QuantityMl = null);

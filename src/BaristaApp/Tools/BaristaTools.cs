using BaristaApp.Api;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace BaristaApp.Tools;

/// <summary>
/// MCP Tools for the Barista MCP App.
///
/// These tools are called both by the AI host directly (MCP Tools demo) and
/// by the embedded barista web UI via /api/beverages and /api/brew (MCP App demo).
///
/// show_barista — entry-point that returns the beverage list.
///   In a full MCP Apps host, add _meta.ui.resourceUri = "http://localhost:5200/"
///   to the tool's schema so the host opens the barista web UI in a webview.
/// </summary>
[McpServerToolType]
public sealed class BaristaTools(AylaApiClient api)
{
    [McpServerTool]
    [Description("Open the De'Longhi Barista interface to brew a coffee. Returns the list of available beverages as an interactive UI.")]
    [McpMeta("ui", JsonValue = """{ "resourceUri": "ui://barista/app.html" }""")]
    public async Task<string> show_barista(CancellationToken ct)
    {
        var beverages = await api.GetAvailableBeveragesAsync(ct);
        if (beverages.Count == 0)
            return "No beverages found. Make sure your machine is connected and powered on.";

        return JsonSerializer.Serialize(
            beverages.Select(b => new { key = b.Key, name = b.Name }),
            JsonOptions.Default);
    }

    [McpServerTool, Description(
        "Returns the list of beverages available on your De'Longhi machine. " +
        "Use the returned 'key' values with the brew_beverage tool.")]
    public async Task<string> get_beverages(CancellationToken ct)
    {
        var beverages = await api.GetAvailableBeveragesAsync(ct);
        if (beverages.Count == 0)
            return "No beverages found. Make sure your machine is connected and powered on.";

        return JsonSerializer.Serialize(
            beverages.Select(b => new { key = b.Key, name = b.Name }),
            JsonOptions.Default);
    }

    [McpServerTool, Description(
        "Brews a beverage on the De'Longhi machine. " +
        "Use 'get_beverages' first to get valid beverage keys.\n\n" +
        "Parameters:\n" +
        "- beverage_key: Key from get_beverages, e.g. 'espresso', 'cappuccino'\n" +
        "- profile: User profile 1-3 (default 2)\n" +
        "- quantity_ml: Optional quantity override in mL.")]
    public async Task<string> brew_beverage(
        [Description("Beverage key from get_beverages (e.g. 'espresso', 'cappuccino')")]
        string beverage_key,
        [Description("User profile 1-3 (default 2)")]
        int profile = 2,
        [Description("Optional quantity in mL to override the saved recipe amount")]
        int? quantity_ml = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(beverage_key))
            return "Error: beverage_key is required.";

        if (profile is < 1 or > 3)
            return "Error: profile must be 1, 2, or 3.";

        if (quantity_ml is < 1 or > 1000)
            return "Error: quantity_ml must be between 1 and 1000.";

        try
        {
            await api.BrewBeverageAsync(beverage_key, profile, quantity_ml, ct);
            var qtyInfo = quantity_ml.HasValue ? $", {quantity_ml}ml" : "";
            return $"Brewing '{beverage_key}' (profile {profile}{qtyInfo})… Enjoy!";
        }
        catch (DelonghiApiException ex)
        {
            return $"Cannot brew: {ex.Message}";
        }
    }

    [McpServerTool, Description(
        "Stops the current beverage preparation on the De'Longhi machine.")]
    public async Task<string> stop_beverage(CancellationToken ct)
    {
        await api.StopBrewAsync(ct);
        return "Stop command sent. Current beverage preparation cancelled.";
    }

    [McpServerTool, Description(
        "Returns the current status of the De'Longhi coffee machine: state, " +
        "active profile, accessory, and any active alarms.")]
    public async Task<string> get_machine_status(CancellationToken ct)
    {
        var monitor = await api.GetMonitorAsync(ct);
        if (monitor is null)
            return "Could not retrieve machine status. Is the machine connected?";

        var result = new
        {
            state         = monitor.MachineState,
            state_code    = monitor.StateCode,
            profile       = monitor.ActiveProfile,
            accessory     = monitor.AccessoryName,
            alarms        = monitor.Alarms,
            blocking      = monitor.BlockingAlarms,
            ready_to_brew = monitor.StateCode == 2 && !monitor.HasBlockingAlarm,
        };
        return JsonSerializer.Serialize(result, JsonOptions.Default);
    }

    [McpServerTool, Description(
        "Powers on the De'Longhi coffee machine. " +
        "The machine will begin heating; it takes about 30-60 seconds to reach ready state.")]
    public async Task<string> power_on(CancellationToken ct)
    {
        await api.PowerOnAsync(ct);
        return "Power-on command sent. The machine is heating up (allow ~30-60 seconds).";
    }

    [McpServerTool, Description(
        "Powers off the De'Longhi coffee machine into standby mode.")]
    public async Task<string> power_off(CancellationToken ct)
    {
        await api.PowerOffAsync(ct);
        return "Power-off command sent. The machine is entering standby.";
    }
}

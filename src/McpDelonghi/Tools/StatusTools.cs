using McpDelonghi.Api;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace McpDelonghi.Tools;

[McpServerToolType]
public sealed class StatusTools(AylaApiClient api)
{
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
        "Returns maintenance metrics: grounds container percentage (0-100), " +
        "filter lifetime percentage, and total descale count.")]
    public async Task<string> get_maintenance_info(CancellationToken ct)
    {
        var counters = await api.GetCountersAsync(ct);
        var result = new
        {
            grounds_percentage  = counters.GetValueOrDefault("grounds_percentage", -1),
            filter_percentage   = counters.GetValueOrDefault("filter_percentage", -1),
            descale_count       = counters.GetValueOrDefault("descale_count", -1),
        };
        return JsonSerializer.Serialize(result, JsonOptions.Default);
    }

    [McpServerTool, Description(
        "Returns lifetime beverage counters: total beverages, espressos, " +
        "cappuccinos, latte macchiatos, and other drinks.")]
    public async Task<string> get_beverage_counters(CancellationToken ct)
    {
        var counters = await api.GetCountersAsync(ct);
        return JsonSerializer.Serialize(counters, JsonOptions.Default);
    }

    [McpServerTool, Description(
        "Returns the list of user profiles configured on the De'Longhi machine. " +
        "Each profile has a number (1-3 are usable with brew_beverage) and a name " +
        "set by the user in the De'Longhi Coffee Link app.")]
    public async Task<string> get_profiles(CancellationToken ct)
    {
        var profiles = await api.GetProfilesAsync(ct);
        if (profiles.Count == 0)
            return "No profiles found. Make sure your machine is connected and powered on.";

        var result = profiles.Select(p => new { number = p.Number, name = p.Name }).ToList();
        return JsonSerializer.Serialize(result, JsonOptions.Default);
    }

    [McpServerTool, Description(
        "Use to explore available properties for development purposes.")]
    public async Task<string> get_raw_properties(CancellationToken ct)
    {
        var props = await api.GetAllRawPropertiesAsync(ct);
        return JsonSerializer.Serialize(props, JsonOptions.Default);
    }
}

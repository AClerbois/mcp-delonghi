using McpDelonghi.Api;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace McpDelonghi.Tools;

[McpServerToolType]
public sealed class BrewTools(AylaApiClient api)
{
    [McpServerTool, Description(
        "Returns the list of beverages available on your De'Longhi machine. " +
        "Use the returned 'key' values with the brew_beverage tool.")]
    public async Task<string> get_beverages(CancellationToken ct)
    {
        var beverages = await api.GetAvailableBeveragesAsync(ct);
        if (beverages.Count == 0)
            return "No beverages found. Make sure your machine is connected and powered on.";

        var result = beverages.Select(b => new { key = b.Key, name = b.Name }).ToList();
        return JsonSerializer.Serialize(result, JsonOptions.Default);
    }

    [McpServerTool, Description(
        "Brews a beverage on the De'Longhi machine. " +
        "Use 'get_beverages' first to get valid beverage keys.\n\n" +
        "The machine must be powered on and in Ready state (no active alarms). " +
        "Beverages requiring a milk module (cappuccino, latte_macchiato, etc.) " +
        "need the Latte Crema accessory attached.\n\n" +
        "Parameters:\n" +
        "- beverage_key: Key from get_beverages, e.g. 'espresso', 'cappuccino', 'lungo'\n" +
        "- profile: User profile number (1-3, default 2)\n" +
        "- quantity_ml: Optional quantity override in mL (overrides the saved recipe amount).\n" +
        "  Applies to: coffee volume for espresso/lungo/americano, milk volume for milk drinks,\n" +
        "  water volume for hot_water/tea. Typical ranges: espresso 20-60, lungo 80-200,\n" +
        "  hot_water 50-400, milk beverages 50-300.")]
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
            return $"Brewing '{beverage_key}' (profile {profile}{qtyInfo})... Enjoy!";
        }
        catch (DelonghiApiException ex)
        {
            return $"Cannot brew: {ex.Message}";
        }
    }

    [McpServerTool, Description(
        "Stops the current beverage preparation on the De'Longhi machine. " +
        "Sends a cancel command — use with care as it discards the current drink.")]
    public async Task<string> stop_beverage(CancellationToken ct)
    {
        await api.StopBrewAsync(ct);
        return "Stop command sent. Current beverage preparation cancelled.";
    }

    [McpServerTool, Description(
        "Returns the full recipe stored on the machine for a given beverage and user profile. " +
        "Shows: coffee/milk/hot-water volume (mL), grind level, temperature (Low/Medium/High), " +
        "and whether pre-ground mode is active.\n\n" +
        "Parameters:\n" +
        "- beverage_key: Key from get_beverages, e.g. 'espresso', 'lungo'\n" +
        "- profile: User profile 1-3 (default 2)")]
    public async Task<string> get_beverage_recipe(
        [Description("Beverage key from get_beverages (e.g. 'espresso', 'cappuccino')")]
        string beverage_key,
        [Description("User profile 1-3 (default 2)")]
        int profile = 2,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(beverage_key))
            return "Error: beverage_key is required.";

        if (profile is < 1 or > 3)
            return "Error: profile must be 1, 2, or 3.";

        try
        {
            var recipe = await api.GetBeverageRecipeAsync(beverage_key, profile, ct);
            return JsonSerializer.Serialize(recipe, JsonOptions.Default);
        }
        catch (DelonghiApiException ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}

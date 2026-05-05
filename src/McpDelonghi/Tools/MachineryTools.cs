using McpDelonghi.Api;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace McpDelonghi.Tools;

[McpServerToolType]
public sealed class MachineryTools(AylaApiClient api)
{
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

    [McpServerTool, Description(
        "Switches the active user profile on the De'Longhi machine (1-4). " +
        "Profiles store personalised recipes, favourite beverages, and settings. " +
        "Use get_profiles to see the names of available profiles.")]
    public async Task<string> set_active_profile(
        [Description("Profile number to activate (1-4)")]
        int profile,
        CancellationToken ct = default)
    {
        if (profile is < 1 or > 4)
            return "Error: profile must be between 1 and 4.";

        try
        {
            await api.SetActiveProfileAsync(profile, ct);
            return $"Profile {profile} activated. The machine has switched to profile {profile}.";
        }
        catch (DelonghiApiException ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}

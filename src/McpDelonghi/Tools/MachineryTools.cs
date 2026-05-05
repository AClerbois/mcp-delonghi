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
}

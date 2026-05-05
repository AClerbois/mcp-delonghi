using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using McpDelonghi.Api.Models;
using McpDelonghi.Auth;
using McpDelonghi.Ecam;
using Microsoft.Extensions.Logging;

namespace McpDelonghi.Api;

public class DelonghiApiException(string message, Exception? inner = null)
    : Exception(message, inner);

/// <summary>
/// Ayla Networks REST API client for De'Longhi coffee machines.
/// Caches the first device's DSN and command property (app_data_request vs data_request).
/// </summary>
public sealed class AylaApiClient
{
    private readonly HttpClient _http;
    private readonly DelonghiAuthService _auth;
    private readonly ILogger<AylaApiClient> _logger;

    // Cached per-session device info
    private string? _dsn;
    private string? _cmdProperty;  // "app_data_request" (Eletta) or "data_request" (PrimaDonna)
    private string? _oemModel;

    public AylaApiClient(HttpClient http, DelonghiAuthService auth, ILogger<AylaApiClient> logger)
    {
        _http = http;
        _auth = auth;
        _logger = logger;
    }

    // ── Device discovery ──────────────────────────────────────────────────

    public async Task<List<AylaDevice>> GetDevicesAsync(CancellationToken ct = default)
    {
        var req = await MakeRequestAsync(HttpMethod.Get, $"{Constants.AylaAdsUrl}/apiv1/devices.json", ct);
        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var devices = new List<AylaDevice>();
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            var d = item.GetProperty("device");
            devices.Add(new AylaDevice(
                Dsn:              d.TryGetString("dsn")             ?? "",
                ProductName:      d.TryGetString("product_name")    ?? d.TryGetString("model") ?? "De'Longhi",
                OemModel:         d.TryGetString("oem_model")       ?? "",
                ConnectionStatus: d.TryGetString("connection_status") ?? "Unknown",
                SwVersion:        d.TryGetString("sw_version")));
        }

        if (devices.Count > 0 && _dsn is null)
        {
            var first = devices[0];
            _dsn      = first.Dsn;
            _oemModel = first.OemModel;
            _cmdProperty = first.OemModel.StartsWith("DL-pd-", StringComparison.OrdinalIgnoreCase)
                ? "data_request"
                : "app_data_request"; // default for Eletta/striker and unknowns
            _logger.LogInformation("Device cached: DSN={Dsn}, Model={Model}, Cmd={Cmd}",
                _dsn, _oemModel, _cmdProperty);
        }

        return devices;
    }

    /// <summary>Returns cached DSN, discovering devices first if needed.</summary>
    public async Task<string> GetDsnAsync(CancellationToken ct = default)
    {
        if (_dsn is not null) return _dsn;
        await GetDevicesAsync(ct);
        return _dsn ?? throw new DelonghiApiException("No De'Longhi device found in your account");
    }

    // ── Properties ───────────────────────────────────────────────────────

    public async Task<Dictionary<string, JsonElement>> GetPropertiesAsync(
        string dsn, string[]? names = null, CancellationToken ct = default)
    {
        var url = $"{Constants.AylaAdsUrl}/apiv1/dsns/{dsn}/properties.json";
        if (names is { Length: > 0 })
        {
            var qs = string.Join("&", names.Select(n => $"names[]={Uri.EscapeDataString(n)}"));
            url = $"{url}?{qs}";
        }

        var req = await MakeRequestAsync(HttpMethod.Get, url, ct);
        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var result = new Dictionary<string, JsonElement>();
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            var prop = item.GetProperty("property");
            var name = prop.TryGetString("name");
            if (name is not null)
                result[name] = prop.Clone();
        }
        return result;
    }

    public async Task<JsonElement?> GetPropertyAsync(string dsn, string name, CancellationToken ct = default)
    {
        var req = await MakeRequestAsync(HttpMethod.Get,
            $"{Constants.AylaAdsUrl}/apiv1/dsns/{dsn}/properties/{name}.json", ct);
        using var resp = await _http.SendAsync(req, ct);
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        resp.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        return doc.RootElement.TryGetProperty("property", out var p) ? p.Clone() : null;
    }

    // ── Commands ─────────────────────────────────────────────────────────

    /// <summary>
    /// Sends an ECAM command. Tries app_data_request first (Eletta Explore),
    /// falls back to data_request on 404.
    /// </summary>
    public async Task SendCommandAsync(string dsn, byte[] ecamBytes, CancellationToken ct = default)
    {
        var b64 = EcamPacket.BuildBase64(ecamBytes);
        _logger.LogDebug("Sending ECAM command: {Hex}", Convert.ToHexString(ecamBytes));

        // Build attempt list based on cached model
        var attempts = (_cmdProperty ?? "app_data_request") == "data_request"
            ? new[] { "data_request", "app_data_request" }
            : new[] { "app_data_request", "data_request" };

        foreach (var prop in attempts)
        {
            var req = await MakeRequestAsync(HttpMethod.Post,
                $"{Constants.AylaAdsUrl}/apiv1/dsns/{dsn}/properties/{prop}/datapoints.json", ct);
            req.Content = JsonContent.Create(new { datapoint = new { value = b64 } });

            using var resp = await _http.SendAsync(req, ct);
            if ((int)resp.StatusCode == 201)
            {
                _cmdProperty = prop; // cache the working property
                _logger.LogInformation("Command sent via {Prop}: {Hex}", prop, Convert.ToHexString(ecamBytes));
                return;
            }
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug("{Prop} returned 404, trying next", prop);
                continue;
            }
            resp.EnsureSuccessStatusCode(); // throws on other errors
        }

        throw new DelonghiApiException("No valid command property found (both endpoints returned 404)");
    }

    /// <summary>Sends the app_device_connected ping to force the machine to push fresh data.</summary>
    public async Task PingConnectedAsync(string dsn, CancellationToken ct = default)
    {
        var b64 = EcamPacket.BuildPingBase64();
        var req = await MakeRequestAsync(HttpMethod.Post,
            $"{Constants.AylaAdsUrl}/apiv1/dsns/{dsn}/properties/app_device_connected/datapoints.json", ct);
        req.Content = JsonContent.Create(new { datapoint = new { value = b64 } });

        using var resp = await _http.SendAsync(req, ct);
        if (resp.StatusCode != System.Net.HttpStatusCode.NotFound)
            resp.EnsureSuccessStatusCode();
    }

    // ── High-level machine operations ─────────────────────────────────────

    public async Task<MonitorData?> GetMonitorAsync(CancellationToken ct = default)
    {
        var dsn = await GetDsnAsync(ct);
        var props = await GetPropertiesAsync(dsn,
            ["app_device_status", "d302_monitor_machine", "d302_monitor"], ct);

        var monitorProp = props.TryGetValue("d302_monitor_machine", out var m) ? m
                        : props.TryGetValue("d302_monitor", out var m2) ? m2
                        : (JsonElement?)null;

        var val = monitorProp?.TryGetString("value");
        return val is not null ? MonitorData.Parse(val) : null;
    }

    public async Task PowerOnAsync(CancellationToken ct = default)
    {
        var dsn = await GetDsnAsync(ct);
        await PingConnectedAsync(dsn, ct);
        await SendCommandAsync(dsn, Constants.PowerOnCmd, ct);
    }

    public async Task PowerOffAsync(CancellationToken ct = default)
    {
        var dsn = await GetDsnAsync(ct);
        await SendCommandAsync(dsn, Constants.PowerOffCmd, ct);
    }

    public async Task StopBrewAsync(CancellationToken ct = default)
    {
        var dsn = await GetDsnAsync(ct);
        var cancelCmd = Crc16.AppendCrc(Constants.CancelCmdBody);
        await SendCommandAsync(dsn, cancelCmd, ct);
    }

    /// <summary>
    /// Brews a beverage by fetching the stored recipe from Ayla and converting it
    /// to a brew command (0x83). Performs pre-brew safety checks first.
    /// </summary>
    public async Task BrewBeverageAsync(string beverageKey, int profile = 2, int? quantityMl = null, CancellationToken ct = default)
    {
        var dsn = await GetDsnAsync(ct);

        // Pre-brew safety check
        var monitor = await GetMonitorAsync(ct);
        if (monitor is not null)
        {
            if (monitor.StateCode == 0)
                throw new DelonghiApiException($"Cannot brew '{beverageKey}': machine is Off. Power on first.");
            if (monitor.StateCode == 3)
                throw new DelonghiApiException($"Cannot brew '{beverageKey}': machine is already Brewing.");
            if (monitor.StateCode is 5 or 8 or 9 or 1 or 6)
                throw new DelonghiApiException(
                    $"Cannot brew '{beverageKey}': machine is busy ({monitor.MachineState}).");
            if (monitor.HasBlockingAlarm)
                throw new DelonghiApiException(
                    $"Cannot brew '{beverageKey}': {string.Join(", ", monitor.BlockingAlarms)}");

            // Milk module check
            if (Constants.MilkBeverages.Contains(beverageKey) && monitor.AccessoryCode < 2)
                throw new DelonghiApiException(
                    $"Cannot brew '{beverageKey}': Latte Crema milk module required " +
                    $"(current accessory: {monitor.AccessoryName}).");
        }

        // Fetch all recipe properties
        var allProps = await GetPropertiesAsync(dsn, ct: ct);

        // Find the recipe property: try requested profile first, then fallback
        byte[]? recipe = null;
        var targets = new[]
        {
            $"_rec_{profile}_{beverageKey}",  // Eletta: d302_rec_2_espresso
            $"_{profile}_rec_{beverageKey}",  // PrimaDonna: d028_2_rec_espresso
        };

        foreach (var (propName, prop) in allProps)
        {
            var val = prop.TryGetString("value");
            if (val is null || val.StartsWith('{')) continue;
            if (targets.Any(t => propName.Contains(t)))
            {
                try { recipe = Convert.FromBase64String(val); break; }
                catch { /* skip malformed */ }
            }
        }

        // Fallback: any profile or default recipe
        if (recipe is null)
        {
            foreach (var (propName, prop) in allProps)
            {
                var val = prop.TryGetString("value");
                if (val is null || val.StartsWith('{')) continue;
                if (propName.Contains($"_rec_{beverageKey}") && !propName.Contains("custom"))
                {
                    try { recipe = Convert.FromBase64String(val); break; }
                    catch { /* skip */ }
                }
            }
        }

        if (recipe is null)
            throw new DelonghiApiException(
                $"Recipe not found for '{beverageKey}'. Is this beverage available on your machine?");

        if (recipe.Length < 8)
            throw new DelonghiApiException($"Recipe for '{beverageKey}' is too short ({recipe.Length} bytes)");

        bool isIced     = beverageKey.StartsWith("i_") || beverageKey.StartsWith("mi_")
                        || beverageKey.StartsWith("over_ice");
        bool isColdBrew = beverageKey.Contains("_cb_");

        // Build quantity override for the relevant ECAM parameter id
        // pid 15 = HOT_WATER, pid 9 = MILK, pid 1 = COFFEE (all 16-bit, in mL)
        Dictionary<int, int>? overrides = null;
        if (quantityMl.HasValue)
        {
            int pid = (beverageKey is "hot_water" or "tea") ? 15
                    : Constants.MilkBeverages.Contains(beverageKey) ? 9
                    : 1;
            overrides = new Dictionary<int, int> { [pid] = quantityMl.Value };
        }

        var brewCmd = EcamPacket.RecipeToBrew(recipe, isIced, isColdBrew, profile, overrides: overrides);

        _logger.LogInformation("Brewing {Beverage} (profile={Profile}): {Hex}",
            beverageKey, profile, Convert.ToHexString(brewCmd));

        await PingConnectedAsync(dsn, ct);
        await SendCommandAsync(dsn, brewCmd, ct);
    }

    /// <summary>Returns the list of beverage keys discovered from Ayla recipe properties.</summary>
    public async Task<List<(string Key, string Name)>> GetAvailableBeveragesAsync(CancellationToken ct = default)
    {
        var dsn = await GetDsnAsync(ct);
        var allProps = await GetPropertiesAsync(dsn, ct: ct);

        var keys = new HashSet<string>();
        foreach (var propName in allProps.Keys)
        {
            if (!propName.Contains("_rec_") || propName.Contains("custom")
                || propName.Contains("priority") || propName.Contains("recipe_custom_name"))
                continue;

            var parts = propName.Split("_rec_", 2);
            if (parts.Length != 2 || !parts[0].StartsWith("d")) continue;

            var rest = parts[1]; // e.g. "2_espresso" or "espresso"
            string bev = (rest.Length > 2 && char.IsDigit(rest[0]) && rest[1] == '_')
                ? rest[2..] // Eletta: strip "2_"
                : rest;     // PrimaDonna: already the key

            if (bev.Length > 0 && !bev.All(char.IsDigit))
                keys.Add(bev);
        }

        return keys.OrderBy(k => k).Select(k =>
        {
            var name = Constants.Beverages.TryGetValue(k, out var meta) ? meta.Name
                     : k.Replace('_', ' ').ToTitleCase();
            return (k, name);
        }).ToList();
    }

    /// <summary>
    /// Returns the list of user profiles with their names.
    ///
    /// Profile names are stored in two Ayla properties:
    ///   d051_profile_name1_3 — profiles 1-3 (3 × 22-byte records)
    ///   d052_profile_name4   — profile 4    (1 × 22-byte record)
    ///
    /// Binary layout per property:
    ///   [0-3]  Header (0xD0, len, cmd, flags)
    ///   [4]    First profile number in this packet
    ///   [5]    Last  profile number in this packet
    ///   Per profile (22 bytes):
    ///     [0]    Leading byte (flags)
    ///     [1-20] UTF-16 LE name, null-padded (10 chars max); 0xFF hi-byte = ECAM end marker
    ///     [21]   Trailing byte
    ///   Last 2 bytes: CRC-16
    /// </summary>
    public async Task<List<(int Number, string Name)>> GetProfilesAsync(CancellationToken ct = default)
    {
        var dsn = await GetDsnAsync(ct);
        var props = await GetPropertiesAsync(dsn,
            ["d051_profile_name1_3", "d052_profile_name4"], ct);

        var profiles = new List<(int Number, string Name)>();

        void ParsePacket(string base64Val)
        {
            byte[] raw;
            try { raw = Convert.FromBase64String(base64Val); }
            catch { return; }

            if (raw.Length < 10) return;

            int startNum = raw[4];
            int endNum   = raw[5];
            int count    = endNum - startNum + 1;
            int offset   = 6;

            for (int i = 0; i < count; i++)
            {
                if (offset + 22 > raw.Length - 2) break;
                var name = ParseEcamName(raw.AsSpan(offset + 1, 20));
                if (name.Length > 0)
                    profiles.Add((startNum + i, name));
                offset += 22;
            }
        }

        if (props.TryGetValue("d051_profile_name1_3", out var p51))
        {
            var val = p51.TryGetString("value");
            if (val is not null) ParsePacket(val);
        }
        if (props.TryGetValue("d052_profile_name4", out var p52))
        {
            var val = p52.TryGetString("value");
            if (val is not null) ParsePacket(val);
        }

        return profiles.OrderBy(p => p.Number).ToList();
    }

    /// <summary>
    /// Decodes a 20-byte UTF-16 LE name buffer from an ECAM profile record.
    /// Stops at null pair (0x00 0x00) or 0xFF hi-byte (ECAM end marker).
    /// When hi-byte = 0xFF, the lo-byte is treated as an ASCII character.
    /// </summary>
    private static string ParseEcamName(ReadOnlySpan<byte> nameBytes)
    {
        var sb = new System.Text.StringBuilder();
        for (int j = 0; j + 1 < nameBytes.Length; j += 2)
        {
            byte lo = nameBytes[j], hi = nameBytes[j + 1];
            if (lo == 0 && hi == 0) break;          // null terminator
            if (hi == 0xFF)
            {
                if (lo != 0xFF) sb.Append((char)lo); // ASCII fallback + end
                break;
            }
            sb.Append((char)(lo | (hi << 8)));
        }
        return sb.ToString().Trim();
    }

    /// <summary>Returns all raw Ayla property names and their current values for diagnostics.</summary>
    public async Task<Dictionary<string, string?>> GetAllRawPropertiesAsync(CancellationToken ct = default)
    {
        var dsn = await GetDsnAsync(ct);
        var props = await GetPropertiesAsync(dsn, ct: ct);
        return props.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.TryGetString("value"));
    }

    /// <summary>Reads beverage counters and maintenance statistics from Ayla properties.</summary>
    public async Task<Dictionary<string, int>> GetCountersAsync(CancellationToken ct = default)
    {
        var dsn = await GetDsnAsync(ct);
        var props = await GetPropertiesAsync(dsn, ct: ct);
        var counters = new Dictionary<string, int>();

        var counterMap = new Dictionary<string, string>
        {
            ["d701_tot_bev_b"]        = "total_beverages",
            ["d704_tot_bev_espressi"] = "total_espressos",
            ["d705_tot_id1_espr"]     = "espresso",
            ["d706_tot_id2_coffee"]   = "coffee",
            ["d710_tot_id7_capp"]     = "cappuccino",
            ["d711_id8_lattmacc"]     = "latte_macchiato",
            ["d712_id9_cafflatt"]     = "caffe_latte",
            ["d715_id12_hotmilk"]     = "hot_milk",
            ["d718_id16_hotwater"]    = "hot_water",
            ["d719_id22_tea"]         = "tea",
            ["d551_cnt_coffee_fondi"] = "grounds_count",
            ["d552_cnt_calc_tot"]     = "descale_count",
            ["d510_ground_cnt_percentage"] = "grounds_percentage",
            ["d513_percentage_usage_fltr"] = "filter_percentage",
        };

        foreach (var (propName, friendlyName) in counterMap)
        {
            if (props.TryGetValue(propName, out var prop))
            {
                var val = prop.TryGetString("value");
                if (val is not null && int.TryParse(val, out var n))
                    counters[friendlyName] = n;
            }
        }
        return counters;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private async Task<HttpRequestMessage> MakeRequestAsync(HttpMethod method, string url, CancellationToken ct)
    {
        var token = await _auth.GetAccessTokenAsync(ct);
        var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("auth_token", token);
        req.Headers.Add("x-ayla-source", "Mobile");
        return req;
    }
}

// ── Extension helpers ─────────────────────────────────────────────────────

file static class Extensions
{
    public static string? TryGetString(this JsonElement el, string property)
        => el.TryGetProperty(property, out var p) && p.ValueKind == JsonValueKind.String
            ? p.GetString() : null;

    public static string? TryGetString(this JsonElement? el, string property)
        => el.HasValue ? el.Value.TryGetString(property) : null;

    public static string ToTitleCase(this string s)
        => string.Join(' ', s.Split(' ').Select(w => w.Length > 0
            ? char.ToUpper(w[0]) + w[1..] : w));
}

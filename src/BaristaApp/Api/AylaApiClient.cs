using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BaristaApp.Api.Models;
using BaristaApp.Auth;
using BaristaApp.Ecam;
using Microsoft.Extensions.Logging;

namespace BaristaApp.Api;

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

    private string? _dsn;
    private string? _cmdProperty;
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
                Dsn:              d.TryGetString("dsn")               ?? "",
                ProductName:      d.TryGetString("product_name")      ?? d.TryGetString("model") ?? "De'Longhi",
                OemModel:         d.TryGetString("oem_model")         ?? "",
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
                : "app_data_request";
            _logger.LogInformation("Device cached: DSN={Dsn}, Model={Model}, Cmd={Cmd}",
                _dsn, _oemModel, _cmdProperty);
        }

        return devices;
    }

    public async Task<string> GetDsnAsync(CancellationToken ct = default)
    {
        if (_dsn is not null) return _dsn;
        await GetDevicesAsync(ct);
        return _dsn ?? throw new DelonghiApiException("No De'Longhi device found in your account");
    }

    // ── Properties ────────────────────────────────────────────────────────

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

    // ── Commands ──────────────────────────────────────────────────────────

    public async Task SendCommandAsync(string dsn, byte[] ecamBytes, CancellationToken ct = default)
    {
        var b64 = EcamPacket.BuildBase64(ecamBytes);
        _logger.LogDebug("Sending ECAM command: {Hex}", Convert.ToHexString(ecamBytes));

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
                _cmdProperty = prop;
                _logger.LogInformation("Command sent via {Prop}: {Hex}", prop, Convert.ToHexString(ecamBytes));
                return;
            }
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug("{Prop} returned 404, trying next", prop);
                continue;
            }
            resp.EnsureSuccessStatusCode();
        }

        throw new DelonghiApiException("No valid command property found (both endpoints returned 404)");
    }

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

    public async Task BrewBeverageAsync(string beverageKey, int profile = 2, int? quantityMl = null, CancellationToken ct = default)
    {
        var dsn = await GetDsnAsync(ct);

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

            if (Constants.MilkBeverages.Contains(beverageKey) && monitor.AccessoryCode < 2)
                throw new DelonghiApiException(
                    $"Cannot brew '{beverageKey}': Latte Crema milk module required " +
                    $"(current accessory: {monitor.AccessoryName}).");
        }

        var allProps = await GetPropertiesAsync(dsn, ct: ct);

        byte[]? recipe = null;
        var targets = new[]
        {
            $"_rec_{profile}_{beverageKey}",
            $"_{profile}_rec_{beverageKey}",
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

            var rest = parts[1];
            string bev = (rest.Length > 2 && char.IsDigit(rest[0]) && rest[1] == '_')
                ? rest[2..]
                : rest;

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

    private static string ParseEcamName(ReadOnlySpan<byte> nameBytes)
    {
        var sb = new System.Text.StringBuilder();
        for (int j = 0; j + 1 < nameBytes.Length; j += 2)
        {
            byte lo = nameBytes[j], hi = nameBytes[j + 1];
            if (lo == 0 && hi == 0) break;
            if (hi == 0xFF)
            {
                if (lo != 0xFF) sb.Append((char)lo);
                break;
            }
            sb.Append((char)(lo | (hi << 8)));
        }
        return sb.ToString().Trim();
    }

    public async Task<Dictionary<string, string?>> GetAllRawPropertiesAsync(CancellationToken ct = default)
    {
        var dsn = await GetDsnAsync(ct);
        var props = await GetPropertiesAsync(dsn, ct: ct);
        return props.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.TryGetString("value"));
    }

    public async Task<Dictionary<string, int>> GetCountersAsync(CancellationToken ct = default)
    {
        var dsn = await GetDsnAsync(ct);
        var props = await GetPropertiesAsync(dsn, ct: ct);
        var counters = new Dictionary<string, int>();

        var counterMap = new Dictionary<string, string>
        {
            ["d701_tot_bev_b"]             = "total_beverages",
            ["d704_tot_bev_espressi"]      = "total_espressos",
            ["d705_tot_id1_espr"]          = "espresso",
            ["d706_tot_id2_coffee"]        = "coffee",
            ["d710_tot_id7_capp"]          = "cappuccino",
            ["d711_id8_lattmacc"]          = "latte_macchiato",
            ["d712_id9_cafflatt"]          = "caffe_latte",
            ["d715_id12_hotmilk"]          = "hot_milk",
            ["d718_id16_hotwater"]         = "hot_water",
            ["d719_id22_tea"]              = "tea",
            ["d551_cnt_coffee_fondi"]      = "grounds_count",
            ["d552_cnt_calc_tot"]          = "descale_count",
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

    public async Task<AylaDevice?> GetDeviceInfoAsync(CancellationToken ct = default)
    {
        var devices = await GetDevicesAsync(ct);
        return devices.Count > 0 ? devices[0] : null;
    }

    public async Task<Dictionary<string, object?>> GetMachineSettingsAsync(CancellationToken ct = default)
    {
        var dsn = await GetDsnAsync(ct);
        var props = await GetPropertiesAsync(dsn,
            ["d281_mach_sett_temperature", "d282_mach_sett_auto_off", "d283_mach_sett_water_hard"], ct);

        var result = new Dictionary<string, object?>();

        byte[]? Decode(string key)
        {
            if (!props.TryGetValue(key, out var p)) return null;
            var v = p.TryGetString("value");
            if (v is null || v.StartsWith('{')) return null;
            try { return Convert.FromBase64String(v); } catch { return null; }
        }

        var tempBytes = Decode("d281_mach_sett_temperature");
        if (tempBytes is { Length: >= 7 })
            result["temperature_unit"] = tempBytes[6] == 0 ? "Celsius" : "Fahrenheit";

        var autoOffBytes = Decode("d282_mach_sett_auto_off");
        if (autoOffBytes is { Length: >= 7 })
            result["auto_off_minutes"] = autoOffBytes[6] == 0 ? "disabled" : (object)autoOffBytes[6];

        var waterBytes = Decode("d283_mach_sett_water_hard");
        if (waterBytes is { Length: >= 7 })
            result["water_hardness"] = waterBytes[6] switch
            {
                1 => "1 (very soft)",
                2 => "2 (soft)",
                3 => "3 (medium)",
                4 => "4 (hard)",
                5 => "5 (very hard)",
                var v => (object)v,
            };

        return result;
    }

    public async Task<Dictionary<string, object>> GetBeverageRecipeAsync(
        string beverageKey, int profile = 2, CancellationToken ct = default)
    {
        var dsn = await GetDsnAsync(ct);
        var allProps = await GetPropertiesAsync(dsn, ct: ct);

        byte[]? recipe = null;
        var targets = new[]
        {
            $"_rec_{profile}_{beverageKey}",
            $"_{profile}_rec_{beverageKey}",
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

        if (recipe is null)
            throw new DelonghiApiException(
                $"Recipe not found for '{beverageKey}' (profile {profile}). " +
                "Use get_beverages to list available keys.");

        if (recipe.Length < 8)
            throw new DelonghiApiException($"Recipe for '{beverageKey}' is too short ({recipe.Length} bytes).");

        var result = new Dictionary<string, object>
        {
            ["beverage_key"] = beverageKey,
            ["profile"]      = profile,
        };
        if (Constants.Beverages.TryGetValue(beverageKey, out var meta))
            result["beverage_name"] = meta.Name;

        var pidNames = new Dictionary<int, string>
        {
            [1]  = "coffee_ml",
            [2]  = "grind_level",
            [3]  = "temperature",
            [4]  = "preground",
            [9]  = "milk_ml",
            [15] = "hot_water_ml",
            [25] = "visible",
            [28] = "accessory",
            [31] = "iced",
            [38] = "cold_brew_intensity",
        };

        var raw = recipe.AsSpan(6, recipe.Length - 8);
        int i = 0;
        while (i < raw.Length)
        {
            int pid = raw[i];
            if (Constants.BigParams.Contains(pid) && i + 2 < raw.Length)
            {
                int val = (raw[i + 1] << 8) | raw[i + 2];
                if (pidNames.TryGetValue(pid, out var pname))
                    result[pname] = val;
                i += 3;
            }
            else if (i + 1 < raw.Length)
            {
                int val = raw[i + 1];
                if (pidNames.TryGetValue(pid, out var pname))
                {
                    object friendly = (pid, val) switch
                    {
                        (3, 0) => "Low",
                        (3, 1) => "Medium",
                        (3, 2) => "High",
                        (4, _) => val != 0,
                        _      => (object)val,
                    };
                    result[pname] = friendly;
                }
                i += 2;
            }
            else break;
        }

        return result;
    }

    public async Task SetActiveProfileAsync(int profile, CancellationToken ct = default)
    {
        if (profile is < 1 or > 4)
            throw new DelonghiApiException("Profile must be between 1 and 4.");

        var dsn = await GetDsnAsync(ct);
        var body = new byte[] { 0x0D, 0x0B, 0x95, 0xF0, (byte)profile, 0xEE, 0x00, 0x00, 0x00, 0x00 };
        var cmd = Crc16.AppendCrc(body);
        await PingConnectedAsync(dsn, ct);
        await SendCommandAsync(dsn, cmd, ct);
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

// ── Extension helpers ─────────────────────────────────────────────────────────

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

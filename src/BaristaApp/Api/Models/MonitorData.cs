namespace BaristaApp.Api.Models;

/// <summary>Parsed MonitorDataV2 from the <c>d302_monitor_machine</c> Ayla property.</summary>
public sealed record MonitorData(
    string MachineState,
    int StateCode,
    int ActiveProfile,
    int AccessoryCode,
    string AccessoryName,
    uint AlarmWord,
    List<string> Alarms,
    List<string> BlockingAlarms,
    bool HasBlockingAlarm)
{
    /// <summary>
    /// Parses the Base64-encoded binary value of d302_monitor_machine.
    ///
    /// MonitorDataV2 byte layout (Eletta Explore / DL-striker):
    ///   [0-3]  Header (0xD0, len, cmd, flags)
    ///   [4]    Active profile
    ///   [5]    Accessory code
    ///   [6]    Switch bits
    ///   [7]    Alarm byte 0 (bits  0-7)
    ///   [8]    Alarm byte 1 (bits  8-15)
    ///   [9]    Machine state
    ///   [10]   Sub-state
    ///   [11]   Extra data
    ///   [12]   Alarm byte 2 (bits 16-23)
    ///   [13]   Alarm byte 3 (bits 24-31)
    /// </summary>
    public static MonitorData? Parse(string base64Value)
    {
        byte[] raw;
        try { raw = Convert.FromBase64String(base64Value); }
        catch { return null; }

        if (raw.Length < 14) return null;

        int profile   = raw[4];
        int accessory = raw[5];
        int stateCode = raw[9];

        uint alarmWord = (uint)raw[7]
                       | ((uint)raw[8]  << 8)
                       | ((uint)raw[12] << 16)
                       | ((uint)raw[13] << 24);

        var alarms         = new List<string>();
        var blockingAlarms = new List<string>();
        foreach (var (bit, meta) in Constants.AlarmBits)
        {
            if ((alarmWord & (1u << bit)) != 0)
            {
                alarms.Add(meta.Name);
                if (meta.Blocking) blockingAlarms.Add(meta.Name);
            }
        }

        string state = Constants.MachineStates.TryGetValue(stateCode, out var s)
            ? s : $"Unknown ({stateCode})";

        string accName = accessory switch
        {
            0 => "None",
            1 => "Hot Water Spout",
            2 => "Latte Crema Hot",
            3 => "Latte Crema Cold",
            _ => $"Unknown ({accessory})",
        };

        return new MonitorData(state, stateCode, profile, accessory, accName,
            alarmWord, alarms, blockingAlarms, blockingAlarms.Count > 0);
    }
}

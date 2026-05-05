namespace BaristaApp.Ecam;

/// <summary>
/// Builds ECAM/Ayla WiFi packets and converts stored recipes to brew commands.
/// Protocol reference: sk7n4k3d/delonghi-ha (MITM-verified against 9 captures).
/// </summary>
public static class EcamPacket
{
    /// <summary>
    /// Wraps raw ECAM bytes in the Ayla WiFi envelope and returns a Base64 string
    /// ready to POST to <c>app_data_request</c> (Eletta Explore / DL-striker models).
    /// Packet = ECAM bytes + Unix timestamp (4 bytes BE) + App signature (4 bytes).
    /// </summary>
    public static string BuildBase64(byte[] ecamBytes)
    {
        var ts = BitConverter.GetBytes((uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        if (BitConverter.IsLittleEndian) Array.Reverse(ts);

        var packet = new byte[ecamBytes.Length + 4 + Constants.AppSignature.Length];
        ecamBytes.CopyTo(packet, 0);
        ts.CopyTo(packet, ecamBytes.Length);
        Constants.AppSignature.CopyTo(packet, ecamBytes.Length + 4);

        return Convert.ToBase64String(packet);
    }

    /// <summary>
    /// Builds the ping payload for <c>app_device_connected</c>:
    /// Unix timestamp (4 bytes BE) + App signature (4 bytes), Base64 encoded.
    /// </summary>
    public static string BuildPingBase64()
    {
        var ts = BitConverter.GetBytes((uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        if (BitConverter.IsLittleEndian) Array.Reverse(ts);

        var packet = new byte[4 + Constants.AppSignature.Length];
        ts.CopyTo(packet, 0);
        Constants.AppSignature.CopyTo(packet, 4);
        return Convert.ToBase64String(packet);
    }

    /// <summary>
    /// Converts a stored recipe (0xD0/0xA6 response packet) to a brew command (0x0D/0x83).
    ///
    /// Conversion rules (verified against 9 MITM captures):
    ///  - Exclude VISIBLE(25) and IDX_LEN(27) — recipe-only fields
    ///  - For iced/cold brew: also exclude COFFEE(1), MILK(9), HOT_WATER(15) quantities
    ///  - For iced:      append ICED(31)=0
    ///  - For cold brew: append ICED(31)=3 + INTENSITY(38)=value
    ///  - Always append RINSE(39)=1
    ///  - End with profile_save byte = (profile &lt;&lt; 2) | 2
    /// </summary>
    public static byte[] RecipeToBrew(byte[] recipe, bool isIced, bool isColdBrew,
        int profile = 2, int intensity = 1, Dictionary<int, int>? overrides = null)
    {
        // Already a brew command
        if (recipe[0] == 0x0D) return recipe;

        byte bevId = recipe[5];
        var raw = recipe.AsSpan(6, recipe.Length - 8); // skip 6-byte header, skip 2-byte CRC

        var excluded = new HashSet<int> { 25, 27 }; // VISIBLE, IDX_LEN
        if (isIced || isColdBrew)
        {
            excluded.Add(1);  // COFFEE
            excluded.Add(9);  // MILK
            excluded.Add(15); // HOT_WATER
        }

        var brewParams = new List<byte>();
        int i = 0;
        while (i < raw.Length)
        {
            int pid = raw[i];
            if (Constants.BigParams.Contains(pid) && i + 2 < raw.Length)
            {
                // 16-bit parameter: [pid][hi][lo]
                if (!excluded.Contains(pid))
                {
                    brewParams.Add(raw[i]);
                    if (overrides is not null && overrides.TryGetValue(pid, out var ov))
                    {
                        brewParams.Add((byte)(ov >> 8));
                        brewParams.Add((byte)(ov & 0xFF));
                    }
                    else
                    {
                        brewParams.Add(raw[i + 1]);
                        brewParams.Add(raw[i + 2]);
                    }
                }
                i += 3;
            }
            else if (i + 1 < raw.Length)
            {
                // 8-bit parameter: [pid][value]
                if (!excluded.Contains(pid))
                {
                    brewParams.Add(raw[i]);
                    brewParams.Add(raw[i + 1]);
                }
                i += 2;
            }
            else break;
        }

        brewParams.AddRange([27, 1]); // IDX_LEN=1

        if (isIced)
            brewParams.AddRange([31, 0]);
        else if (isColdBrew)
            brewParams.AddRange([31, 3, 38, (byte)intensity]);

        brewParams.AddRange([39, 1]); // RINSE=1

        int total = 6 + brewParams.Count + 1 + 2; // header(6) + params + profile_save(1) + crc(2)
        var body = new List<byte>
        {
            0x0D,
            (byte)(total - 1),
            0x83,
            0xF0,
            bevId,
            0x03,
        };
        body.AddRange(brewParams);
        body.Add((byte)((profile << 2) | 2)); // profile_save

        return Crc16.AppendCrc([.. body]);
    }
}

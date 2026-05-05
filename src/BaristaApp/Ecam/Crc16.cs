namespace BaristaApp.Ecam;

/// <summary>CRC-16/SPI-FUJITSU (AUG-CCITT): init 0x1D0F, poly 0x1021, no XOR output.</summary>
public static class Crc16
{
    public static byte[] Compute(ReadOnlySpan<byte> data)
    {
        int crc = 0x1D0F;
        foreach (var b in data)
        {
            crc ^= b << 8;
            for (int i = 0; i < 8; i++)
                crc = (crc & 0x8000) != 0 ? (crc << 1) ^ 0x1021 : crc << 1;
        }
        crc &= 0xFFFF;
        return [(byte)(crc >> 8), (byte)(crc & 0xFF)];
    }

    public static byte[] AppendCrc(byte[] body)
    {
        var crc = Compute(body);
        var result = new byte[body.Length + 2];
        body.CopyTo(result, 0);
        result[body.Length]     = crc[0];
        result[body.Length + 1] = crc[1];
        return result;
    }
}

namespace McpDelonghi;

/// <summary>
/// Public app-level credentials extracted from the official De'Longhi Coffee Link Android app.
/// These are NOT user secrets — every user of the official app shares the same keys.
/// They identify the application to Gigya and Ayla Networks, similar to an embedded API key.
/// </summary>
public static class Constants
{
    // ── Gigya (De'Longhi identity provider, always EU1) ──────────────────
    public const string GigyaApiKey = "4_DRIMLu7jk9bkKwpRRoQOuw";
    public const string GigyaBaseUrl = "https://accounts.eu1.gigya.com";

    // ── Ayla Networks IoT cloud (EU region) ──────────────────────────────
    public const string AylaAppId = "DLonghiCoffeeIdKit-sQ-id";
    public const string AylaAppSecret = "DLonghiCoffeeIdKit-HT6b0VNd4y6CSha9ivM5k8navLw";
    public const string AylaUserUrl = "https://user-field-eu.aylanetworks.com";
    public const string AylaAdsUrl = "https://ads-eu.aylanetworks.com";

    // ── ECAM packet constants ─────────────────────────────────────────────
    /// <summary>App signature appended to every command for newer models (Eletta Explore).</summary>
    public static readonly byte[] AppSignature = [0x20, 0x40, 0x35, 0xEF];

    // Pre-built power commands (ECAM body + CRC-16/SPI-FUJITSU, verified from MITM captures)
    public static readonly byte[] PowerOnCmd  = Convert.FromHexString("0d07840f02015512");
    public static readonly byte[] PowerOffCmd = Convert.FromHexString("0d07840f01010041");

    // Cancel command body (CRC appended at runtime)
    public static readonly byte[] CancelCmdBody = [0x0D, 0x04, 0x8F];

    // ── Machine states (MonitorDataV2 byte[9]) ────────────────────────────
    public static readonly IReadOnlyDictionary<int, string> MachineStates =
        new Dictionary<int, string>
        {
            [0] = "Off",
            [1] = "Turning On",
            [2] = "Idle",
            [3] = "Brewing",
            [4] = "Error",
            [5] = "Descaling",
            [6] = "Heating",
            [7] = "Ready",
            [8] = "Rinsing",
            [9] = "Going to sleep",
        };

    // ── Alarm bit definitions (32-bit word from MonitorDataV2) ───────────
    public static readonly IReadOnlyDictionary<int, AlarmMeta> AlarmBits =
        new Dictionary<int, AlarmMeta>
        {
            [0]  = new("Water Tank Empty",          Blocking: true),
            [1]  = new("Grounds Container Full",     Blocking: true),
            [2]  = new("Descale Needed",             Blocking: false),
            [3]  = new("Replace Water Filter",       Blocking: false),
            [4]  = new("Coffee Ground Too Fine",     Blocking: false),
            [5]  = new("Coffee Beans Empty",         Blocking: true),
            [6]  = new("Machine Service Required",   Blocking: true),
            [7]  = new("Heater Probe Failure",       Blocking: true),
            [8]  = new("Too Much Coffee",            Blocking: false),
            [9]  = new("Infuser Motor Failure",      Blocking: true),
            [10] = new("Steamer Probe Failure",      Blocking: true),
            [11] = new("Drip Tray Missing",          Blocking: true),
            [12] = new("Hydraulic Problem",          Blocking: true),
            [13] = new("Water Tank Missing",         Blocking: true),
            [14] = new("Clean Milk Knob",            Blocking: false),
            [15] = new("Coffee Beans Empty 2",       Blocking: false),
            [16] = new("Cleaning Needed",            Blocking: false),
            [17] = new("Bean Hopper Absent",         Blocking: true),
            [18] = new("Grid Missing",               Blocking: true),
        };

    // ── Known beverages ───────────────────────────────────────────────────
    public static readonly IReadOnlyDictionary<string, BeverageMeta> Beverages =
        new Dictionary<string, BeverageMeta>
        {
            // Hot coffee
            ["espresso"]              = new("Espresso",              1),
            ["coffee"]                = new("Coffee",                2),
            ["long_coffee"]           = new("Long Coffee",           3),
            ["2x_espresso"]           = new("Double Espresso",       4),
            ["doppio"]                = new("Doppio+",               5),
            ["americano"]             = new("Americano",             6),
            ["cappuccino"]            = new("Cappuccino",            7),
            ["latte_macchiato"]       = new("Latte Macchiato",       8),
            ["caffelatte"]            = new("Caffe Latte",           9),
            ["flat_white"]            = new("Flat White",            10),
            ["espresso_macchiato"]    = new("Espresso Macchiato",    11),
            ["hot_milk"]              = new("Hot Milk",              12),
            ["cappuccino_doppio"]     = new("Cappuccino Doppio+",    13),
            ["cappuccino_reverse"]    = new("Cappuccino Mix",        15),
            ["hot_water"]             = new("Hot Water",             16),
            ["espresso_lungo"]        = new("Espresso Lungo",        20),
            ["tea"]                   = new("Tea",                   22),
            ["coffee_pot"]            = new("Coffee Pot",            23),
            ["cortado"]               = new("Cortado",               24),
            ["long_black"]            = new("Long Black",            25),
            ["brew_over_ice"]         = new("Brew Over Ice",         27),
            // Iced
            ["i_americano"]           = new("Iced Americano",        50),
            ["i_cappuccino"]          = new("Iced Cappuccino",       51),
            ["i_latte_macch"]         = new("Iced Latte Macchiato",  52),
            ["i_capp_mix"]            = new("Iced Cappuccino Mix",   53),
            ["i_flatwhite"]           = new("Iced Flat White",       54),
            ["over_ice_espr"]         = new("Iced Espresso",         57),
            // Cold brew
            ["a_cb_coffee"]           = new("Cold Brew Coffee",      120),
            ["b_cb_coffee_ess"]       = new("Cold Brew Essence",     121),
            ["c_cb_coffee_pot"]       = new("Cold Brew Pot",         122),
        };

    // Beverages that require the Latte Crema milk module (accessory param id 28 > 1)
    public static readonly IReadOnlySet<string> MilkBeverages =
        new HashSet<string>(["cappuccino", "latte_macchiato", "caffelatte", "flat_white",
                              "espresso_macchiato", "hot_milk", "cortado",
                              "cappuccino_doppio", "cappuccino_reverse"]);

    // 16-bit parameter IDs in ECAM recipe packets (3 bytes instead of 2)
    public static readonly IReadOnlySet<int> BigParams = new HashSet<int>([1, 9, 15]); // COFFEE, MILK, HOT_WATER
}

public readonly record struct AlarmMeta(string Name, bool Blocking);
public readonly record struct BeverageMeta(string Name, int DrinkId);

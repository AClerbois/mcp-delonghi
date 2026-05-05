# ECAM Protocol — De'Longhi Eletta Explore (DL-striker / Ayla WiFi)

> **Source:** Reverse engineering via MITM captures (9 verified) + analysis of the  
> De'Longhi Coffee Link Android app and community projects.  
> Tested on: Eletta Explore (OEM model `DL-striker`).

---

## Table of Contents

1. [Authentication Stack](#1-authentication-stack)
2. [Ayla Networks IoT Transport](#2-ayla-networks-iot-transport)
3. [Ayla WiFi Envelope](#3-ayla-wifi-envelope)
4. [ECAM Packet Structure](#4-ecam-packet-structure)
5. [ECAM Command Reference](#5-ecam-command-reference)
6. [CRC-16 Algorithm](#6-crc-16-algorithm)
7. [Monitor Data (0x75)](#7-monitor-data-0x75)
8. [Recipe Properties (0xA6)](#8-recipe-properties-0xa6)
9. [Brew Command (0x83)](#9-brew-command-0x83)
10. [Power Commands (0x84)](#10-power-commands-0x84)
11. [Machine Settings (0x95)](#11-machine-settings-0x95)
12. [Profile Names (0xA4)](#12-profile-names-0xa4)
13. [Known Ayla Properties](#13-known-ayla-properties)
14. [Beverage ID Table](#14-beverage-id-table)
15. [Alarm Bits](#15-alarm-bits)

---

## 1. Authentication Stack

```
User ──► Gigya EU1 ──► Ayla Networks EU ──► Machine (WiFi)
```

### Gigya (Identity Provider)

| Field | Value |
|-------|-------|
| Region | EU1 (always) |
| Base URL | `https://accounts.eu1.gigya.com` |
| API Key | `4_DRIMLu7jk9bkKwpRRoQOuw` |
| Endpoint | `POST /accounts.login` → returns `id_token` |

The `id_token` is then exchanged for an Ayla access token.

### Ayla Networks (IoT Cloud)

| Field | Value |
|-------|-------|
| App ID | `DLonghiCoffeeIdKit-sQ-id` |
| App Secret | `DLonghiCoffeeIdKit-HT6b0VNd4y6CSha9ivM5k8navLw` |
| User URL | `https://user-field-eu.aylanetworks.com` |
| ADS URL | `https://ads-eu.aylanetworks.com` |

These are **public app-level credentials** embedded in the Coffee Link app — not user secrets.

**Token exchange:**
```
POST https://user-field-eu.aylanetworks.com/api/v1/token_sign_in
{
  "app_id": "...", "app_secret": "...",
  "provider": "sso", "token": "<gigya_id_token>"
}
→ { "access_token": "...", "refresh_token": "..." }
```

**Requests use:**
```
Authorization: auth_token <access_token>
x-ayla-source: Mobile
```

---

## 2. Ayla Networks IoT Transport

### Device Discovery

```
GET /apiv1/devices.json
→ [{ "device": { "dsn": "AC000...", "oem_model": "DL-striker-xxx", ... } }]
```

### Reading Properties

```
GET /apiv1/dsns/{dsn}/properties.json
GET /apiv1/dsns/{dsn}/properties/{name}.json
GET /apiv1/dsns/{dsn}/properties.json?names[]=prop1&names[]=prop2
```

Properties are named `d{NNN}_{friendly_name}` (e.g. `d302_monitor_machine`).  
Values are Base64-encoded ECAM binary packets.

### Writing / Sending Commands

```
POST /apiv1/dsns/{dsn}/properties/{prop_name}/datapoints.json
{ "datapoint": { "value": "<base64_ayla_envelope>" } }
→ 201 Created (success)
```

**Command property name by model:**

| OEM Model prefix | Property name |
|------------------|---------------|
| `DL-pd-` (PrimaDonna) | `data_request` |
| All others (Eletta, striker) | `app_data_request` |

Both endpoints are tried on first send; the working one is cached.

### Ping (force data push)

```
POST /apiv1/dsns/{dsn}/properties/app_device_connected/datapoints.json
{ "datapoint": { "value": "<ping_base64>" } }
```

Ping payload = Unix timestamp (4 bytes BE) + App signature (4 bytes), Base64-encoded.

---

## 3. Ayla WiFi Envelope

Every ECAM command sent to `app_data_request` is wrapped:

```
[ ECAM bytes ] [ Unix timestamp 4 bytes BE ] [ App signature 4 bytes ]
```

**App signature:** `20 40 35 EF`

Base64-encode the full envelope before POSTing.

Example for power-on (`0D 07 84 0F 02 01 55 12`):
```
ECAM:      0D 07 84 0F 02 01 55 12
Timestamp: 68 1A 3C 00   (Unix 2026-05-05)
Signature: 20 40 35 EF
→ Base64(all 16 bytes)
```

> Response values from the machine (read properties) do **not** include the envelope —  
> they are raw ECAM packets directly Base64-encoded.

---

## 4. ECAM Packet Structure

All ECAM packets share the same 6-byte header:

```
Byte  0    : Direction marker
               0x0D = Host → Machine (command)
               0xD0 = Machine → Host (response/data)
Byte  1    : Total length − 1  (i.e. len(packet) − 1, excluding CRC)
Byte  2    : Command / response type (see table below)
Byte  3    : Flags
               0x0F = standard read response
               0xF0 = write command / profile-keyed
Byte  4    : Sub-index or profile number (command-specific)
Byte  5    : Beverage ID or sub-command ID (command-specific)
...        : Payload (TLV parameters or raw data)
Last 2     : CRC-16/SPI-FUJITSU (big-endian)
```

---

## 5. ECAM Command Reference

| Cmd byte | Direction | Name | Description |
|----------|-----------|------|-------------|
| `0x75`   | M→H | MONITOR | Machine status (MonitorDataV2) |
| `0x83`   | H→M | BREW | Start brewing a beverage |
| `0x84`   | H→M | POWER | Power on / off |
| `0x8F`   | H→M | CANCEL | Stop current beverage |
| `0x95`   | M→H | MACHINE_SETTINGS | Settings (temperature, auto-off, profile…) |
| `0xA1`   | M→H | DEVICE_INFO | Serial number, bean system params |
| `0xA4`   | M→H | PROFILE_NAMES | User profile names (UTF-16 LE) |
| `0xA6`   | M→H | RECIPE | Stored beverage recipe |

---

## 6. CRC-16 Algorithm

**CRC-16/SPI-FUJITSU** (also called CRC-16/AUG-CCITT):

```
Init  : 0x1D0F
Poly  : 0x1021
Input / Output XOR : none (no bit reversal, no final XOR)
```

```csharp
int crc = 0x1D0F;
foreach (byte b in data)
{
    crc ^= b << 8;
    for (int i = 0; i < 8; i++)
        crc = (crc & 0x8000) != 0 ? (crc << 1) ^ 0x1021 : crc << 1;
}
crc &= 0xFFFF;
// append: [(byte)(crc >> 8), (byte)(crc & 0xFF)]
```

The CRC covers all bytes **from byte 0** (the direction marker) up to but not including the CRC itself.

---

## 7. Monitor Data (0x75)

Ayla property: `d302_monitor_machine` (or `d302_monitor` on older firmware).

```
Byte  0    : 0xD0 (machine → host)
Byte  1    : length − 1
Byte  2    : 0x75
Byte  3    : 0x0F
Byte  4    : Active profile number (1–4)
Byte  5    : Accessory code
               0 = None
               1 = Hot Water Spout
               2 = Latte Crema Hot
               3 = Latte Crema Cold
Byte  6    : Switch bits
Byte  7    : Alarm word bits  0–7
Byte  8    : Alarm word bits  8–15
Byte  9    : Machine state code (see table)
Byte 10    : Sub-state
Byte 11    : Extra data
Byte 12    : Alarm word bits 16–23
Byte 13    : Alarm word bits 24–31
Last 2     : CRC-16
```

### Machine State Codes

| Code | State |
|------|-------|
| 0 | Off |
| 1 | Turning On |
| 2 | Idle |
| 3 | Brewing |
| 4 | Error |
| 5 | Descaling |
| 6 | Heating |
| 7 | Ready |
| 8 | Rinsing |
| 9 | Going to sleep |

---

## 8. Recipe Properties (0xA6)

Ayla property naming:
- Eletta: `d{NNN}_rec_{profile}_{beverage_key}` — e.g. `d102_rec_2_espresso`
- PrimaDonna: `d{NNN}_{profile}_rec_{beverage_key}` — e.g. `d028_2_rec_espresso`

```
Byte  0    : 0xD0
Byte  1    : length − 1
Byte  2    : 0xA6
Byte  3    : 0xF0
Byte  4    : Profile number (1–4)
Byte  5    : Beverage ID (see table)
Bytes 6..N-2 : TLV parameters (see below)
Last 2     : CRC-16
```

### TLV Parameters

Each parameter is either:
- **8-bit:** `[pid 1 byte][value 1 byte]`
- **16-bit (BigParams):** `[pid 1 byte][value_hi 1 byte][value_lo 1 byte]`

**16-bit (BigParam) parameter IDs:** `1` (COFFEE), `9` (MILK), `15` (HOT_WATER)

| PID | Name | Unit | Notes |
|-----|------|------|-------|
| 1 | COFFEE | mL (16-bit) | Coffee volume |
| 2 | GRIND | — | Grind level |
| 3 | TEMP | — | Temperature setting |
| 4 | PREGROUND | — | Pre-ground flag |
| 5 | GRINDER_CLEAN | — | |
| 9 | MILK | mL (16-bit) | Milk volume |
| 15 | HOT_WATER | mL (16-bit) | Hot water volume |
| 25 | VISIBLE | — | Recipe-only; excluded from brew cmd |
| 27 | IDX_LEN | — | Recipe-only; always forced to 1 in brew cmd |
| 28 | ACCESSORY | — | Milk module type |
| 31 | ICED | — | 0=iced, 3=cold brew |
| 38 | INTENSITY | — | Cold brew intensity |
| 39 | RINSE | — | Always 1 in brew commands |

---

## 9. Brew Command (0x83)

Converted from a recipe packet (0xA6 → 0x83):

```
Byte  0    : 0x0D (host → machine)
Byte  1    : length − 1
Byte  2    : 0x83
Byte  3    : 0xF0
Byte  4    : Beverage ID
Byte  5    : 0x03
Bytes 6..N-3 : TLV parameters (subset of recipe, see rules below)
Byte  N-2  : Profile save byte = (profile << 2) | 2
Last 2     : CRC-16
```

### Recipe → Brew Conversion Rules

1. **Exclude** `VISIBLE (25)` and `IDX_LEN (27)` — recipe-only fields
2. For **iced** beverages (key starts with `i_`, `mi_`, or `over_ice`):
   - Exclude `COFFEE (1)`, `MILK (9)`, `HOT_WATER (15)`
   - Append `ICED (31) = 0`
3. For **cold brew** beverages (key contains `_cb_`):
   - Exclude `COFFEE (1)`, `MILK (9)`, `HOT_WATER (15)`
   - Append `ICED (31) = 3`, `INTENSITY (38) = value`
4. Always append `IDX_LEN (27) = 1` and `RINSE (39) = 1`
5. End with `profile_save = (profile << 2) | 2`

### Quantity Override (mL)

To override a volume, replace the 16-bit value of the relevant BigParam before building the brew command:

| Beverage type | PID to override |
|---------------|-----------------|
| `hot_water`, `tea` | 15 (HOT_WATER) |
| Milk beverages (cappuccino, latte, etc.) | 9 (MILK) |
| All other coffee | 1 (COFFEE) |

---

## 10. Power Commands (0x84)

Pre-built, CRC already included:

| Command | Hex |
|---------|-----|
| Power ON | `0D 07 84 0F 02 01 55 12` |
| Power OFF | `0D 07 84 0F 01 01 00 41` |

Cancel (stop beverage) body, CRC appended at runtime:
```
0D 04 8F
```

> **Always send a ping** (`app_device_connected`) before power-on and brew commands to ensure  
> the machine is listening.

---

## 11. Machine Settings (0x95)

Ayla properties `d280`–`d286`. All use:
```
0xD0, len-1, 0x95, flags, sub_index, setting_id, value_bytes..., CRC
```

| Ayla Property | Setting ID | Description |
|---------------|-----------|-------------|
| `d280_mach_sett_pin` | `0xD2` | PIN lock |
| `d281_mach_sett_temperature` | `0x3D` | Temperature unit |
| `d282_mach_sett_auto_off` | `0x3E` | Auto-off timer |
| `d283_mach_sett_water_hard` | `0x32` | Water hardness |
| `d284_mach_sett_user_conf` | `0x3F` | User configuration |
| `d285_mach_sett_radio_conf` | `0x2D` | Radio/WiFi config |
| `d286_mach_sett_profile` | `0xEE` | Active profile selection (`flags=0xF0`, `sub=profile_num`) |

### Switching Active Profile

`d286_mach_sett_profile` observed value for profile 3:
```
D0 0B 95 F0 03 EE 00 00 00 00 {CRC}
                ^^
                └── profile number (1–4)
```

> **Status: not yet verified as writable.** The property appears to be read-only from Ayla.  
> Profile switching may require sending a direct ECAM command to the machine.  
> Further investigation needed.

---

## 12. Profile Names (0xA4)

| Ayla Property | Profiles |
|---------------|---------|
| `d051_profile_name1_3` | Profiles 1, 2, 3 |
| `d052_profile_name4` | Profile 4 |

```
Byte  0    : 0xD0
Byte  1    : length − 1
Byte  2    : 0xA4
Byte  3    : 0xF0
Byte  4    : First profile number in packet
Byte  5    : Last  profile number in packet
Per profile (22 bytes each):
  [0]      : Leading flags byte
  [1..20]  : UTF-16 LE name, null-padded (max 10 chars)
             0xFF hi-byte = ECAM end marker (lo-byte is ASCII char, then stop)
  [21]     : Trailing byte
Last 2     : CRC-16
```

### Decoding Example

```
d051 hex: D0 49 A4 F0 01 03
  Profile 1 at offset 6+1 = 7:  41 00 6E 00 61 00 69 00 73 00 ... → "Anais"
  Profile 2 at offset 6+23:     41 00 64 00 72 00 69 00 00 00 ... → "Adri"
  Profile 3 at offset 6+45:     41 00 75 00 74 00 72 00 65 00 ... → "Autre"

d052 hex: D0 1D A4 F0 04 04
  Profile 4 at offset 7:        45 00 78 00 70 00 6C 00 6F 00 72 00 65 00 72 00 20 00 34 FF FF
                                 → "Explorer 4" (0x34=ASCII '4', followed by 0xFF end marker)
```

---

## 13. Known Ayla Properties

### Status & Monitoring

| Property | Cmd | Description |
|----------|-----|-------------|
| `d302_monitor_machine` | 0x75 | Machine state, profile, accessory, alarms |
| `d302_monitor` | 0x75 | Alternate name (older firmware) |
| `app_device_status` | — | Connection status |

### Recipes (per profile × beverage)

Pattern: `d{NNN}_rec_{profile}_{beverage_key}`  
Example: `d102_rec_2_espresso`, `d116_rec_2_hot_water`

| Profile | Ayla prefix range |
|---------|-------------------|
| 1 | d059–d102 |
| 2 | d102–d145 |
| 3 | d145–d188 |

### Profile Names

| Property | Description |
|----------|-------------|
| `d051_profile_name1_3` | Names for profiles 1, 2, 3 |
| `d052_profile_name4` | Name for profile 4 |
| `d053_custom_name_13` | Custom recipe names 1–3 |
| `d054_custom_name_46` | Custom recipe names 4–6 |

### Machine Settings

| Property | Description |
|----------|-------------|
| `d280_mach_sett_pin` | PIN |
| `d281_mach_sett_temperature` | Temperature |
| `d282_mach_sett_auto_off` | Auto-off |
| `d283_mach_sett_water_hard` | Water hardness |
| `d284_mach_sett_user_conf` | User config |
| `d285_mach_sett_radio_conf` | Radio config |
| `d286_mach_sett_profile` | Active profile |

### Counters & Maintenance

| Property | Description |
|----------|-------------|
| `d701_tot_bev_b` | Total beverages |
| `d704_tot_bev_espressi` | Total espressos |
| `d705_tot_id1_espr` | Espresso count |
| `d706_tot_id2_coffee` | Coffee count |
| `d710_tot_id7_capp` | Cappuccino count |
| `d711_id8_lattmacc` | Latte macchiato count |
| `d712_id9_cafflatt` | Caffe latte count |
| `d715_id12_hotmilk` | Hot milk count |
| `d718_id16_hotwater` | Hot water count |
| `d719_id22_tea` | Tea count |
| `d510_ground_cnt_percentage` | Grounds container % full |
| `d513_percentage_usage_fltr` | Filter lifetime % used |
| `d551_cnt_coffee_fondi` | Grounds eject count |
| `d552_cnt_calc_tot` | Descale count |

### Device Info

| Property | Description |
|----------|-------------|
| `d270_serialnumber` | Serial number |
| `d260_beansystem_par` | Bean system parameters |

---

## 14. Beverage ID Table

| ID (hex) | Key | Name | Category |
|----------|-----|------|----------|
| 0x01 | espresso | Espresso | Hot coffee |
| 0x02 | regular / coffee | Coffee | Hot coffee |
| 0x03 | long_coffee | Long Coffee | Hot coffee |
| 0x04 | 2x_espresso | Double Espresso | Hot coffee |
| 0x05 | doppio_pl | Doppio+ | Hot coffee |
| 0x06 | americano | Americano | Hot coffee |
| 0x07 | cappuccino | Cappuccino | Milk |
| 0x08 | latte_macch | Latte Macchiato | Milk |
| 0x09 | caffelatte | Caffe Latte | Milk |
| 0x0A | flat_white | Flat White | Milk |
| 0x0B | espr_macch | Espresso Macchiato | Milk |
| 0x0C | hot_milk | Hot Milk | Milk |
| 0x0D | capp_doppio_pl | Cappuccino Doppio+ | Milk |
| 0x0F | capp_reverse | Cappuccino Mix | Milk |
| 0x10 | hot_water | Hot Water | Other |
| 0x14 | espresso_lungo | Espresso Lungo | Hot coffee |
| 0x16 | tea | Tea | Other |
| 0x17 | coffee_pot | Coffee Pot | Hot coffee |
| 0x18 | cortado | Cortado | Milk |
| 0x19 | long_black | Long Black | Hot coffee |
| 0x1B | brew_over_ice | Brew Over Ice | Iced |
| 0x32 | i_americano | Iced Americano | Iced |
| 0x33 | i_cappuccino | Iced Cappuccino | Iced |
| 0x34 | i_latte_macch | Iced Latte Macchiato | Iced |
| 0x35 | i_capp_mix | Iced Cappuccino Mix | Iced |
| 0x36 | i_flatwhite | Iced Flat White | Iced |
| 0x37 | i_coldmilk | Iced Cold Milk | Iced |
| 0x38 | i_caffelatte | Iced Caffe Latte | Iced |
| 0x39 | over_ice_espr | Iced Espresso | Iced |
| 0x50 | m_americano | M Americano | Mobile |
| 0x51 | m_cappuccino | M Cappuccino | Mobile |
| 0x52 | m_latte_macch | M Latte Macchiato | Mobile |
| 0x53 | m_caffelatte | M Caffe Latte | Mobile |
| 0x54 | m_capp_mix | M Cappuccino Mix | Mobile |
| 0x55 | m_flat_white | M Flat White | Mobile |
| 0x56 | m_hot_milk | M Hot Milk | Mobile |
| 0x64 | mi_over_ice | Mi Over Ice | Mobile Iced |
| 0x65 | mi_americano | Mi Americano | Mobile Iced |
| 0x66 | mi_capp | Mi Cappuccino | Mobile Iced |
| 0x67 | mi_latte_macch | Mi Latte Macchiato | Mobile Iced |
| 0x68 | mi_cafflatt | Mi Caffe Latte | Mobile Iced |
| 0x69 | mi_capp_mix | Mi Cappuccino Mix | Mobile Iced |
| 0x6A | mi_flat_white | Mi Flat White | Mobile Iced |
| 0x6B | mi_cold_milk | Mi Cold Milk | Mobile Iced |
| 0x78 | a_cb_coffee | Cold Brew Coffee | Cold Brew |
| 0x79 | b_cb_coffee_ess | Cold Brew Essence | Cold Brew |
| 0x7A | c_cb_coffee_pot | Cold Brew Pot | Cold Brew |
| 0x7B | d_cb_latte | Cold Brew Latte | Cold Brew |
| 0x7C | e_cb_cappuccino | Cold Brew Cappuccino | Cold Brew |
| 0x8C | f_cb_mug | Cold Brew Mug | Cold Brew |
| 0x8D | g_cb_latte_mug | Cold Brew Latte Mug | Cold Brew |
| 0x8E | h_cb_capp_mug | Cold Brew Cappuccino Mug | Cold Brew |

---

## 15. Alarm Bits

32-bit alarm word assembled from bytes 7, 8, 12, 13 of MonitorDataV2:

```
alarmWord = byte[7] | (byte[8] << 8) | (byte[12] << 16) | (byte[13] << 24)
```

| Bit | Name | Blocking |
|-----|------|----------|
| 0 | Water Tank Empty | ✅ |
| 1 | Grounds Container Full | ✅ |
| 2 | Descale Needed | ❌ |
| 3 | Replace Water Filter | ❌ |
| 4 | Coffee Ground Too Fine | ❌ |
| 5 | Coffee Beans Empty | ✅ |
| 6 | Machine Service Required | ✅ |
| 7 | Heater Probe Failure | ✅ |
| 8 | Too Much Coffee | ❌ |
| 9 | Infuser Motor Failure | ✅ |
| 10 | Steamer Probe Failure | ✅ |
| 11 | Drip Tray Missing | ✅ |
| 12 | Hydraulic Problem | ✅ |
| 13 | Water Tank Missing | ✅ |
| 14 | Clean Milk Knob | ❌ |
| 15 | Coffee Beans Empty 2 | ❌ |
| 16 | Cleaning Needed | ❌ |
| 17 | Bean Hopper Absent | ✅ |
| 18 | Grid Missing | ✅ |

**Blocking** alarms prevent brewing. Non-blocking alarms are warnings only.

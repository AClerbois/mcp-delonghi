# mcp-delonghi

> ⚠️ **Disclaimer:** This is an **unofficial, community-made** MCP server. It is not affiliated with, endorsed by, or supported by De'Longhi in any way. This project is **purely educational** — its goal is to explore how MCP servers can interact with IoT devices and reverse-engineered cloud APIs. Use at your own risk.

An [MCP](https://modelcontextprotocol.io) server that lets LLMs control a **De'Longhi Eletta Explore** (and compatible models) coffee machine over WiFi via the De'Longhi Coffee Link cloud (Gigya + Ayla Networks).

Works with Claude Desktop, VS Code Copilot, Cursor, and any other MCP-compatible client.

Built on the official [MCP C# SDK 2.x](https://github.com/modelcontextprotocol/csharp-sdk), which implements the **MCP 2026-07-28** specification (stateless protocol core, `server/discover`, Mcp-Method/Mcp-Name headers, extensions framework). Clients still on the 2025-11-25 revision are served through the SDK's automatic down-level fallback.

## Tools

| Tool | Description |
|---|---|
| `get_machine_status` | Current state, active profile, accessory, alarms |
| `get_maintenance_info` | Grounds %, filter %, descale count |
| `get_beverage_counters` | Lifetime counts per drink type |
| `get_profiles` | List user profiles (number + name) configured in the Coffee Link app |
| `power_on` | Power on the machine |
| `power_off` | Power off into standby |
| `get_beverages` | List beverages available on your machine |
| `brew_beverage` | Brew a beverage (with pre-brew safety checks). Optional `quantity_ml` overrides the saved recipe volume (coffee, milk, or hot water depending on beverage type) |
| `stop_beverage` | Cancel current preparation |

## Prerequisites

- .NET 10 SDK
- A De'Longhi machine registered in the **De'Longhi Coffee Link** app (Eletta Explore, Dinamica Plus, PrimaDonna Soul, etc.)
- The Coffee Link app **must be closed** when using the MCP server (the app holds an exclusive LAN session while open)

## Setup

### 1. Install

**From NuGet (recommended):**

```sh
dotnet tool install --global AClerbois.MCP.DelonghiConnector
```

**From source:**

```sh
dotnet build src/McpDelonghi/McpDelonghi.csproj -c Release
```

### 2. Configure credentials

Set the following environment variables (never commit credentials):

```sh
DELONGHI_EMAIL=your@email.com
DELONGHI_PASSWORD=yourpassword
```

### 3. Add to your MCP client

**Claude Desktop** (`claude_desktop_config.json`) — NuGet install:

```json
{
  "mcpServers": {
    "delonghi": {
      "command": "mcp-delonghi",
      "env": {
        "DELONGHI_EMAIL": "your@email.com",
        "DELONGHI_PASSWORD": "yourpassword"
      }
    }
  }
}
```

**VS Code** (`.vscode/mcp.json`) — NuGet install:

```json
{
  "servers": {
    "delonghi": {
      "type": "stdio",
      "command": "mcp-delonghi",
      "env": {
        "DELONGHI_EMAIL": "your@email.com",
        "DELONGHI_PASSWORD": "yourpassword"
      }
    }
  }
}
```

**VS Code** (`.vscode/mcp.json`) — from source:

```json
{
  "servers": {
    "delonghi": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "${workspaceFolder}/src/McpDelonghi", "-c", "Release"],
      "env": {
        "DELONGHI_EMAIL": "your@email.com",
        "DELONGHI_PASSWORD": "yourpassword"
      }
    }
  }
}
```

## Barista App (MCP Apps demo)

`src/BaristaApp` is a second, demo-oriented server exposing a **subset** of the tools above (`get_machine_status`, `power_on`, `power_off`, `get_beverages`, `brew_beverage`, `stop_beverage`) over **Streamable HTTP** (`POST /mcp`, stateless by default), plus an interactive barista UI delivered through the [MCP Apps](https://github.com/modelcontextprotocol/ext-apps) extension (`ModelContextProtocol.Extensions.Apps`):

- `show_barista` (BaristaApp only) is annotated with `[McpAppUi(ResourceUri = "ui://barista/app.html")]`
- the HTML is served as the `ui://barista/app.html` resource with the `text/html;profile=mcp-app` MIME type
- `WithMcpApps()` advertises the `io.modelcontextprotocol/ui` extension; hosts without MCP Apps support fall back to the text result

```sh
dotnet run --project src/BaristaApp   # MCP endpoint on http://localhost:5200/mcp
```

## Protocol notes

Authentication flow: Gigya SSO (EU1) → Ayla Networks IoT.  
Commands use the ECAM binary protocol, CRC-16/SPI-FUJITSU, wrapped in a timestamped Base64 envelope sent to the `app_data_request` Ayla property.

## License

MIT

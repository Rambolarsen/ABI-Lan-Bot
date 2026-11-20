# ABI-Lan-Bot

A Discord bot built with .NET 10 and Discord.Net.

## Features

- **Ping Command**: Check if the bot is responsive with `!ping`
- **Help Command**: Display available commands with `!help`
- **Info Command**: Show bot information with `!info`

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- A Discord Bot Token (see setup instructions below)

## Getting Your Discord Bot Token

1. Go to the [Discord Developer Portal](https://discord.com/developers/applications)
2. Click "New Application" and give it a name
3. Go to the "Bot" section in the left sidebar
4. Click "Add Bot" and confirm
5. Under the TOKEN section, click "Copy" to copy your bot token
6. Enable "MESSAGE CONTENT INTENT" under Privileged Gateway Intents
7. Go to OAuth2 > URL Generator
8. Select scopes: `bot`
9. Select bot permissions: `Send Messages`, `Read Messages/View Channels`, `Read Message History`
10. Copy the generated URL and open it in your browser to invite the bot to your server

## Setup

1. Clone this repository:
   ```bash
   git clone https://github.com/Rambolarsen/ABI-Lan-Bot.git
   cd ABI-Lan-Bot
   ```

2. Navigate to the project directory:
   ```bash
   cd ABILanBot
   ```

3. Create an `appsettings.json` file from the example:
   ```bash
   cp appsettings.example.json appsettings.json
   ```

4. Edit `appsettings.json` and add your Discord bot token:
   ```json
   {
     "Discord": {
       "Token": "YOUR_BOT_TOKEN_HERE"
     }
   }
   ```

5. Build the project:
   ```bash
   dotnet build
   ```

6. Run the bot:
   ```bash
   dotnet run
   ```

## Usage

Once the bot is running and invited to your server, you can use these commands:

- `!ping` - The bot will respond with "Pong! 🏓"
- `!help` - Displays all available commands
- `!info` - Shows information about the bot

## Project Structure

```
ABILanBot/
├── ABILanBot.csproj          # Project file with dependencies
├── Program.cs                 # Main bot implementation
├── appsettings.example.json  # Configuration template
└── appsettings.json          # Your configuration (not in git)
```

## Dependencies

- **Discord.Net** (3.16.0) - Discord API wrapper for .NET
- **Microsoft.Extensions.Configuration** (8.0.0) - Configuration framework
- **Microsoft.Extensions.Configuration.Json** (8.0.0) - JSON configuration provider

## Development

To modify the bot:

1. Edit `Program.cs` to add new commands or features
2. Build and test your changes:
   ```bash
   dotnet build
   dotnet run
   ```

## Security

**Important**: Never commit your `appsettings.json` file with your actual bot token to version control. The token is like a password to your bot. The `.gitignore` file is configured to exclude this file.

## License

This project is open source and available under the MIT License.

## CS2 server controls (Spillvert)

The bot adds a `/cs2` slash-command group via `Modules/Cs2Module.cs`, backed by a
minimal Source RCON client in `Services/Cs2RconService.cs`.

Commands:

- `/cs2 status` — public map, player count (when reported), and connectivity.
- `/cs2 mode ArmsRace` — sets `game_type 1` / `game_mode 0` and loads `ar_pool_day`.
- `/cs2 mode Deathmatch` — sets `game_type 1` / `game_mode 2` and loads `de_dust2`.
- `/cs2 mode Casual` — sets `game_type 0` / `game_mode 0` and loads `de_mirage`.
- `/cs2 mode Competitive` — sets `game_type 0` / `game_mode 1` and loads `de_mirage`.
- `/cs2 map de_mirage` — changes to a named installed map.
- `/cs2 restartmatch` — restarts the **match**, not the server process.

All commands except `status` require Discord **Manage Server** permission.
Map names are restricted to letters, numbers and underscores to prevent arbitrary
commands being injected via Discord. No generic RCON console is exposed.

### Configuration

Include these settings in the bot's **local**, gitignored `appsettings.json`:

```json
"Cs2": {
  "Host": "oslo21.spillvert.no",
  "Port": 27020
}
```

Set `CS2_RCON_PASSWORD` **on the machine running the bot** to the password
already configured as `rcon_password` on Spillvert. Do not add the secret to
the repository, `appsettings.example.json`, or Discord messages.

For local testing, in PowerShell (the value remains private in this example):

```powershell
$env:CS2_RCON_PASSWORD = Read-Host "Spillvert RCON password"
dotnet run --project ABILanBot
```

The password environment variable must also be configured wherever the bot
runs in production (service, container, or machine) before restarting the bot.

The server startup command line must include `-usercon`; a non-empty
`rcon_password` must be loaded by the server. The server must accept TCP on
port `27020` from the **bot host**, not only from your PC.

**Caveats:** Source RCON is unencrypted. Use a strong, unique RCON password
and restrict network access if the host permits it. Switching modes applies
on the next map load, may disconnect players, and is *not* permanent across
a full server restart: Spillvert's predefined startup profile remains the
startup default. The module reports commands sent, not a guaranteed completed
mode switch; confirm with `/cs2 status` and an in-game test.

### Manual smoke test

1. Run `dotnet build` and deploy the bot with the environment secret set.
2. Use `/cs2 status` to confirm RCON authentication and verify the map.
3. As a Discord user with Manage Server permission, try `/cs2 mode ArmsRace`.
4. Verify the server loads `ar_pool_day` and that Gun Game actually works.
5. Try `/cs2 mode Competitive` only once players are ready to leave the warmup.
6. Restart the game server via Spillvert and verify which startup mode it loads.

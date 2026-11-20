# ABI-Lan-Bot

A Discord LAN party bot built with .NET 10 LTS and Discord.Net. It creates randomized teams and makes game night a little more chaotic.

## Commands

### LAN fun (slash commands)

- **/gamepick** — Randomly choose the next game from CS2, AoE2, Rocket League, Golf With Your Friends, Overcooked and Mario Kart.
  - Optional games parameter: a comma-separated list of 2–20 custom game names, e.g. CS2, Rocket League, Mario Kart.
- **/challenge** — Get a friendly randomized party challenge. Optionally choose a game category.
- **/lanbingo** — Get a randomized 3x3 bingo card with a free center square. Run again for a new card.
- **/dadjoke** — Get a random dad joke (requires an internet connection).

### Teams and voice channels (slash commands)

- **/scrimmage** — Split members of the configured lobby into random teams (optionally move them).
- **/returntolobby** — Move people from bot-created game room voice channels back into the lobby.
- **/cleanupchannels** — Remove empty bot-created game room voice channels (optional force-delete).

### Text commands

- **!ping** — Check if the bot is alive.
- **!help** — Display command help.
- **!info** — Display bot and runtime information.

Slash commands register to the configured LAN guild when the bot starts. They are not text commands.

## Prerequisites

- .NET **10** SDK and runtime, kept current with .NET 10 LTS security patches.
- A Discord application and bot token.

## Setup

1. Clone the repository and open the application directory:

   ```bash
   git clone https://github.com/Rambolarsen/ABI-Lan-Bot.git
   cd ABI-Lan-Bot/ABILanBot
   ```

2. Copy appsettings.example.json to appsettings.json.
3. Set `Discord:Token` to the bot token, `Discord:GuildId` to your Discord server ID, and `GameLobbyChannelName` to the lobby voice channel. The example guild ID belongs to the original ABI LAN server; replace it for other servers.
4. To use `!` commands, enable the privileged **Message Content Intent** in the Discord Developer Portal and set `Discord:EnableMessageContentIntent` to `true` (the default). For a slash-only bot set it to `false`. The bot will not request that privileged intent or process text commands.

   **Upgrade note:** Existing installations must add `Discord:GuildId` to their local appsettings.json before deploying this branch.
5. Invite the bot with bot and applications.commands OAuth2 scopes. Give it View Channels and Send Messages for responses. For team splitting and cleanup, also grant Move Members and Manage Channels.
6. Build and run:

   ```bash
   # From the ABILanBot directory, build and test the entire solution.
   dotnet restore ../ABILanBot.sln
   dotnet build ../ABILanBot.sln --configuration Release --no-restore
   dotnet test ../ABILanBot.sln --configuration Release --no-build

   # Run from ABILanBot so appsettings.json is found.
   dotnet run --no-restore
   ```

Guild-specific slash commands register using `Discord:GuildId` in appsettings.json. Updates usually become available shortly after the bot reconnects.

## Dependencies

- Discord.Net **3.20.2**
- Microsoft.Extensions.Configuration, .Json, .DependencyInjection and .Http **10.0.15**
- Target framework: **net10.0**

GitHub Actions verifies restore, build and automated tests on pull requests. Dependabot checks NuGet packages and workflow actions weekly.

## Security

Never commit appsettings.json or a real Discord token. The .gitignore excludes local configuration.

## Development

The bot's slash commands live in Modules, with reusable logic and service registration in Services. `LanFunService` is stateless and needs no external API. `ABILanBot.Tests` verifies game choices, challenge categories, bingo invariants, Discord intent options and slash command dependency resolution. From the `ABILanBot` directory, run the entire solution test suite with `dotnet test ../ABILanBot.sln` (or run `dotnet test ABILanBot.sln` from the repository root).

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
$secret = Read-Host "Spillvert RCON password" -AsSecureString
$env:CS2_RCON_PASSWORD = [System.Net.NetworkCredential]::new("", $secret).Password
dotnet run --no-restore
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

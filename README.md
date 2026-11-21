# ABI-Lan-Bot

A Discord LAN party bot built with .NET 10 LTS and Discord.Net. It creates randomized teams and makes game night a little more chaotic.

## LAN day on Windows

Run the bot only during LAN events. No Docker, Windows service or VPS required.

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) on your personal Windows PC.
2. Clone or update the repository; keep your existing gitignored `ABILanBot/appsettings.json` when updating.
3. Configure `Discord.Token`, `Discord.GuildId` and `GameLobbyChannelName` there. Keep `Discord.EnableMessageContentIntent=true` if you want the legacy `!` commands.
4. Double-click **Start ABI LAN Bot.cmd** at the repository root, or run `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\start-bot.ps1` from the repository root. The bypass is process-scoped and does not modify your permanent execution policy.
5. If the bot has a CS2 RCON host configured, the launcher offers a hidden password prompt (unless the `CS2_RCON_PASSWORD` environment variable is already set). Press Enter to skip CS2 controls.
6. Wait for the Discord connection message and try `/gamepick` or `/cs2 status`. Keep the window open during the LAN; press **Ctrl+C** to stop it.

The launcher verifies the .NET 10 SDK, builds Release, validates configuration using the application's JSON parser (including JSON comments), launches from the correct working directory, and restarts the bot after a nonzero exit with a five-second delay. Normal clean exits do not restart.

For troubleshooting, run `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\start-bot.ps1 -NoRestart` to disable retries, or use `-SkipRconPrompt` to skip the CS2 password prompt and `-ValidateOnly` to check configuration and the .NET 10 SDK without launching the bot. The double-click `.cmd` launcher also forwards these switches when run from a terminal. GitHub Actions runs validation checks on a Windows runner. Poll votes are held by Discord, not the local process. The script does not print or save credentials. Avoid running multiple copies at the same time.

**Important:** GitHub Actions does not deploy the bot. Update the checkout on the personal PC containing your local `appsettings.json` before running it.

## Commands

### LAN fun (slash commands)

- **/gamepick** — Randomly choose the next game from CS2, AoE2, Rocket League, Golf With Your Friends, Overcooked and Mario Kart.
  - Optional games parameter: a comma-separated list of 2–20 custom game names, e.g. CS2, Rocket League, Mario Kart.
- **/challenge** — Get a friendly randomized party challenge. Optionally choose a game category.
- **/lanbingo** — Get a randomized 3x3 bingo card with a free center square. Run again for a new card.
- **/dadjoke** — Get a random dad joke (requires an internet connection).

### Middagsavstemning i Sandvika (slash-kommando)

- **/mat stem** — Starter en Discord-avstemning om **hvor vi bestiller middag fra**.
  - `timer`: avstemningen varer 1–24 timer (standard 2).
  - Ingen valg for lunsj eller snacks; heller ingen fritekstrestauranter.
  - Alternativer (sjekket mot Wolts publiserte leveringsmenyer for Sandvika 20. november 2026): Domino's Pizza Sandvika, Sandvika Pizza og Kebab, EL GRECO Sandvika, WU Restaurant Sandvika, Zouq Sandvika, El Camino Sandvika og Steam & Stir.
  - Botmeldingen viser lenker til menyene. Alle får én stemme og kan endre denne før fristen. Discord holder orden på stemmene og avslutter avstemningen.
  - **Viktig:** Levering må bekreftes for LAN-lokalets konkrete adresse og klokkeslett ved bestilling. Listen er kuratert, ikke en sanntidssjekk. Botten bestiller ikke mat og avgjør ikke stemmelikhet automatisk.

### LAN game voting and awards (guild slash commands)

- **/spill stem** — start a native Discord poll for the next game. Optional `timer` (1–24 hours, default 1) and `spill` (2–10 comma-separated game names). Uses the same default games as `/gamepick`. Every voter gets one changeable vote.
- **/award stem** — an organizer with **Manage Server** permission starts a native poll for a LAN award. Supply `pris`, e.g. `MVP`, and 2–10 comma-separated `nominerte`, e.g. `Alice, Bob, Charlie`; optional `timer` (1–24 hours). Collect nominations in the channel before launching the final poll; nominees cannot be added after posting.
- Discord manages votes and expiry, so a bot restart does **not** erase the active polls. Poll creation is not automated into a tournament or ranking system; no separate state database is needed for these features.
- Permissions: game voting is available to LAN members; award poll creation requires Manage Server. Both require **Create Polls** for the bot; neither is published to DMs.

### Teams and voice channels (slash commands)

- **/scrimmage** — Split members of the configured lobby into random teams (optionally move them).
- **/returntolobby** — Move people from bot-created game room voice channels back into the lobby.
- **/cleanupchannels** — Remove empty bot-created game room voice channels (optional force-delete).

### Text commands

- **!ping** — Check if the bot is alive.
- **!help** — Display command help.
- **!info** — Display bot and runtime information.

Administrasjonskommandoer registreres kun på LAN-serveren. /gamepick, /challenge, /lanbingo og /dadjoke registreres globalt og kan brukes i DM til botten.

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
5. Invite the bot with bot and applications.commands OAuth2 scopes. Give it View Channels and Send Messages for responses; grant **Create Polls** for `/mat stem`. For team splitting and cleanup, also grant Move Members and Manage Channels.
6. Build and run:

   ```bash
   # From the ABILanBot directory, build and test the entire solution.
   dotnet restore ../ABILanBot.sln
   dotnet build ../ABILanBot.sln --configuration Release --no-restore
   dotnet test ../ABILanBot.sln --configuration Release --no-build

   # Run from ABILanBot so appsettings.json is found.
   dotnet run --no-restore
   ```

Guild-only commands register using Discord:GuildId; safe fun commands are global (server and bot DM). Global command changes may take longer to appear. Startup logs command counts and Discord registration errors.

## Dependencies

- Discord.Net **3.20.2**
- Microsoft.Extensions.Configuration, .Json, .DependencyInjection and .Http **10.0.15**
- Target framework: **net10.0**

GitHub Actions verifies restore, build and automated tests on pull requests. Dependabot checks NuGet packages and workflow actions weekly.

## Security

Never commit appsettings.json or a real Discord token. The .gitignore excludes local configuration.

## Development

The bot's slash commands live in Modules, with reusable logic and service registration in Services. `FoodPollService` validates poll options without contacting Discord; `FoodModule` posts native Discord polls. `LanFunService` is stateless and needs no external API. `ABILanBot.Tests` verifies game choices, challenge categories, bingo invariants, Discord intent options and slash command dependency resolution. From the `ABILanBot` directory, run the entire solution test suite with `dotnet test ../ABILanBot.sln` (or run `dotnet test ABILanBot.sln` from the repository root).

### DM og RCON-feilsøking

`/gamepick`, `/challenge`, `/lanbingo` og `/dadjoke` fungerer i serverkanaler og DM til botten for brukere med en felles server. Disse fire kommandoene publiseres globalt. `/cs2`, `/mat`, `/scrimmage`, `/returntolobby` og `/cleanupchannels` forblir kun på den konfigurerte LAN-serveren.

RCON gir nå egne feilmeldinger for manglende passord, avvist passord, nettverksfeil, tidsavbrudd og ugyldige/avbrutte svar. Rå RCON-svar og detaljer fra nettverksunntak sendes aldri til Discord.

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
- `/cs2 preset Warmup` — starts warmup on the current map (requires `bekreft: true`).
- `/cs2 preset Competitive` — switches to competitive mode and reloads de_mirage (requires `bekreft: true`).
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

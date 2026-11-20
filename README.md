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

   git clone https://github.com/Rambolarsen/ABI-Lan-Bot.git
   cd ABI-Lan-Bot/ABILanBot

2. Copy appsettings.example.json to appsettings.json.
3. Add your bot token under Discord:Token and set GameLobbyChannelName to the name of your lobby voice channel.
4. In the Discord Developer Portal, enable the Message Content privileged intent for the legacy ! text commands. For slash commands alone, that intent is not needed.
5. Invite the bot with bot and applications.commands OAuth2 scopes. Give it View Channels and Send Messages for responses. For team splitting and cleanup, also grant Move Members and Manage Channels.
6. Build and run:

   dotnet restore
   dotnet build --configuration Release
   dotnet run

Note: The Discord guild ID used for slash-command registration is currently defined in Program.cs; change it if you are deploying to a different server. Once connected, Discord may take a short time to display new slash commands.

## Dependencies

- Discord.Net **3.20.2**
- Microsoft.Extensions.Configuration, .Json, .DependencyInjection and .Http **10.0.15**
- Target framework: **net10.0**

GitHub Actions verifies restore and build on pull requests. Dependabot checks NuGet packages and workflow actions weekly.

## Security

Never commit appsettings.json or a real Discord token. The .gitignore excludes local configuration.

## Development

The bot's Discord slash commands live in Modules, while reusable logic is in Services. LanFunService is stateless and needs no external API.


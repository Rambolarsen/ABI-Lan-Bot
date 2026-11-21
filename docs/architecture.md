# Bot architecture and behavior

This is a compact, code-grounded map for a coding agent. Read the relevant source file before changing behavior; this document is not a replacement for it.

## Startup and configuration

- `ABILanBot/Program.cs` builds configuration from `appsettings.json` relative to the **process working directory**, creates services through `BotServiceRegistration`, starts `DiscordSocketClient` and `InteractionService`, and handles `!ping`, `!help`, and `!info`.
- Discord login requires `Discord:Token`. `GameLobbyChannelName` is used by `TeamsModule`. See `ABILanBot/appsettings.example.json` for a non-secret example.
- On Ready, `CommandRegistrationPlan` publishes only `LanFunModule` and `DadJokeModule` globally (server and bot DM contexts). `TeamsModule`, `Cs2Module`, and `FoodModule` remain guild-only using `Discord:GuildId`. Registration logs command counts and errors. Never publish RCON or admin commands globally.
- The client requests the privileged `MessageContent` gateway intent when `Discord:EnableMessageContentIntent` is `true` (the default if the key is absent). Set `Discord:EnableMessageContentIntent` to `false` for a slash-only deployment to avoid requesting the intent. Text commands require this setting enabled and the intent enabled in Discord's Developer Portal; slash commands do not depend on that intent.

## Command and service boundaries

| Area | Entry point | Responsibility |
| --- | --- | --- |
| Slash commands | `Modules/TeamsModule.cs` | `/scrimmage`, `/returntolobby`, `/cleanupchannels`; constructs responses and invokes services |
| Slash commands | `Modules/DadJokeModule.cs` | `/dadjoke`; delegates HTTP retrieval |
| LAN fun commands | `Modules/LanFunModule.cs` | `/gamepick`, `/challenge`, `/lanbingo`; delegates generation to `LanFunService` |
| LAN fun logic | `Services/LanFunService.cs` | Game selection, challenge selection and bingo content |
| Food voting | `Modules/FoodModule.cs` and `Services/FoodPollService.cs` | Publishes a fixed list of verified Sandvika delivery menus via `/mat stem`; Discord owns votes, expiry and results |
| Bot configuration | `Services/BotConfiguration.cs` | Reads and validates guild and message-content settings |
| Dependency injection | `Services/BotServiceRegistration.cs` | Production/test service registrations and socket configuration |
| Team allocation | `Services/TeamService.cs` | Shuffles connected users and distributes them round-robin |
| Voice channels | `Services/VoiceChannelService.cs` | Finds/creates team channels, caches them in memory, deletes eligible cached channels |
| Member moves | `Services/MemberService.cs` | Changes members' voice channels using Discord API |
| External HTTP | `Services/DadJokeService.cs` | Calls icanhazdadjoke.com via an injected `HttpClient`; returns fallback text on failure |
| CS2 interactions | `Modules/Cs2Module.cs` | `/cs2 connect` (public join details), `/cs2 status`, `/cs2 mode`, `/cs2 preset`, `/cs2 map`, `/cs2 restartmatch`; enforces `ManageGuild` on changes |
| RCON transport | `Services/Cs2RconService.cs` | Source RCON authentication, bounded packet handling, serialized administration, mode/map commands |
| CS2 join details | `Services/Cs2ConnectInfo.cs` | Validated public game endpoint and optional join password, never the RCON secret |
| Result model | `Entities/TeamResult.cs` | Holds team allocation success, error, and groups |

## Important behavior constraints

- `TeamsModule` finds the configured voice lobby by name, omits bot users, and delegates team creation and optional moves.
- Team voice channels are named `Game room N`. The current channel cache is **in-memory and keyed by channel name**, not by guild; it does not survive a restart. Verify channel ownership and guild scope before expanding reuse or cleanup.
- `/cleanupchannels` can delete channels; its `force-delete` parameter may delete occupied ones. `/returntolobby` moves users. These require careful permission design and explicit approval for live tests.
- `MemberService` catches individual move failures and logs them; callers should not assume every move succeeded just because the method returned.
- `TeamService.CreateRandomTeams` adapts `SocketGuildUser` input to the Discord-independent `TeamAllocator.Split<T>` (Fisher-Yates plus round-robin). `ABILanBot.Tests/TeamAllocatorTests.cs` uses seeded randomness to check reproducibility, balanced sizes, no missing/duplicate members and empty lobby behavior. A bot-level integration test is still needed to verify Discord effects.
- Do not infer that `/maketeams` exists from old help text. The current team-splitting command is `/scrimmage`; LAN fun commands are registered from `LanFunModule`.

## CS2 / Source RCON (implemented)

- `Cs2Module` registers the guild-only `/cs2` group. `status` reports selected, non-sensitive fields extracted by `Cs2StatusParser` from the RCON response (including the root spawngroup map format used by CS2); `connect` reads public join details from configuration without RCON access; both are public within the guild. `mode`, `preset`, `map` and `restartmatch` require Discord `ManageGuild`. Do not assume that status is an authorization check for administrative actions.
- `Cs2RconService` takes `Cs2:Host` and `Cs2:Port` from configuration and reads `CS2_RCON_PASSWORD` from the bot process environment. Never commit, return or log that password.
- The client uses fresh TCP connections, packet size/output limits and timeouts, and serializes administrative actions with a semaphore. Map names are constrained to a lowercase alphanumeric/underscore pattern; do not replace this with an arbitrary console command endpoint.
- Source RCON traffic is **unencrypted**. Limit host/network exposure. Sending a map or mode command is not proof the game server applied it, particularly when it disconnects during a map transition.
- `ABILanBot.Tests/Cs2RconServiceTests.cs` uses an in-process loopback TCP server to test multi-packet status responses and completion detection, with no production CS2 connection. It is not a full CS2 integration suite.
- Preserve Discord permission checks, input validation and secret handling when modifying the command surface. For offline tests, use an isolated fake RCON server; never issue real commands or move players merely to verify a code change.

See the [README CS2 section](../README.md#cs2-server-controls-spillvert) for deployment prerequisites and the [verification guide](verification.md) for test selection.

See [verification](verification.md) for the check matrix and [AGENTS.md](../AGENTS.md) for the working agreement.

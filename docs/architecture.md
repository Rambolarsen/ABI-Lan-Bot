# Bot architecture and behavior

This is a compact, code-grounded map for a coding agent. Read the relevant source file before changing behavior; this document is not a replacement for it.

## Startup and configuration

- `ABILanBot/Program.cs` builds configuration from `appsettings.json` relative to the **process working directory**, creates services through `BotServiceRegistration`, starts `DiscordSocketClient` and `InteractionService`, and handles `!ping`, `!help`, and `!info`.
- Discord login requires `Discord:Token`. `GameLobbyChannelName` is used by `TeamsModule`. See `ABILanBot/appsettings.example.json` for a non-secret example.
- On Ready, `Program.cs` registers slash commands to the guild specified by `Discord:GuildId` via `BotConfiguration.GetGuildId` (not globally). Do not hard-code guild IDs.
- The client requests the privileged `MessageContent` gateway intent when `Discord:EnableMessageContentIntent` is `true` (the default if the key is absent). Set `Discord:EnableMessageContentIntent` to `false` for a slash-only deployment to avoid requesting the intent. Text commands require this setting enabled and the intent enabled in Discord's Developer Portal; slash commands do not depend on that intent.

## Command and service boundaries

| Area | Entry point | Responsibility |
| --- | --- | --- |
| Slash commands | `Modules/TeamsModule.cs` | `/scrimmage`, `/returntolobby`, `/cleanupchannels`; constructs responses and invokes services |
| Slash commands | `Modules/DadJokeModule.cs` | `/dadjoke`; delegates HTTP retrieval |
| LAN fun commands | `Modules/LanFunModule.cs` | `/gamepick`, `/challenge`, `/lanbingo`; delegates generation to `LanFunService` |
| LAN fun logic | `Services/LanFunService.cs` | Game selection, challenge selection and bingo content |
| Food voting | `Modules/FoodModule.cs` and `Services/FoodPollService.cs` | Validates food choices and posts native Discord single-vote polls; Discord owns votes, expiry and results |
| Bot configuration | `Services/BotConfiguration.cs` | Reads and validates guild and message-content settings |
| Dependency injection | `Services/BotServiceRegistration.cs` | Production/test service registrations and socket configuration |
| Team allocation | `Services/TeamService.cs` | Shuffles connected users and distributes them round-robin |
| Voice channels | `Services/VoiceChannelService.cs` | Finds/creates team channels, caches them in memory, deletes eligible cached channels |
| Member moves | `Services/MemberService.cs` | Changes members' voice channels using Discord API |
| External HTTP | `Services/DadJokeService.cs` | Calls icanhazdadjoke.com via an injected `HttpClient`; returns fallback text on failure |
| Result model | `Entities/TeamResult.cs` | Holds team allocation success, error, and groups |

## Important behavior constraints

- `TeamsModule` finds the configured voice lobby by name, omits bot users, and delegates team creation and optional moves.
- Team voice channels are named `Game room N`. The current channel cache is **in-memory and keyed by channel name**, not by guild; it does not survive a restart. Verify channel ownership and guild scope before expanding reuse or cleanup.
- `/cleanupchannels` can delete channels; its `force-delete` parameter may delete occupied ones. `/returntolobby` moves users. These require careful permission design and explicit approval for live tests.
- `MemberService` catches individual move failures and logs them; callers should not assume every move succeeded just because the method returned.
- `TeamService.CreateRandomTeams` is coupled to `SocketGuildUser` and randomness; consider extracting a pure, deterministic team allocator as a **separate tested change** before adding sophisticated balancing logic.
- Do not infer that `/maketeams` exists from old help text. The current team-splitting command is `/scrimmage`; LAN fun commands are registered from `LanFunModule`.

## Future CS2 / RCON integration

No RCON integration exists on `main` as of this review. If introduced, separate Discord authorization and command parsing from a narrow RCON client, allowlist server actions (e.g. match mode, map change) rather than exposing free-form console commands, keep credentials outside source control, use timeouts, and test against a fake client before any live server.

See [verification](verification.md) for the check matrix and [AGENTS.md](../AGENTS.md) for the working agreement.

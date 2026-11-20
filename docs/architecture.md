# Bot architecture and behavior

This is a compact, code-grounded map for a coding agent. Read the relevant source file before changing behavior; this document is not a replacement for it.

## Startup and configuration

- `ABILanBot/Program.cs` builds configuration from `appsettings.json` relative to the **process working directory**, sets up dependency injection, starts `DiscordSocketClient` and `InteractionService`, and handles `!ping`, `!help`, and `!info`.
- Discord login requires `Discord:Token`. `GameLobbyChannelName` is used by `TeamsModule`. See `ABILanBot/appsettings.example.json` for a non-secret example.
- On Ready, `Program.cs` currently registers slash commands to **one hard-coded guild ID** (not globally). Avoid hard-coding additional guild IDs; make any new environment-specific values configurable.
- The client requests `MessageContent` gateway intent. Running a real bot also depends on Discord-side permissions and intent settings.

## Command and service boundaries

| Area | Entry point | Responsibility |
| --- | --- | --- |
| Slash commands | `Modules/TeamsModule.cs` | `/scrimmage`, `/returntolobby`, `/cleanupchannels`; constructs responses and invokes services |
| Slash commands | `Modules/DadJokeModule.cs` | `/dadjoke`; delegates HTTP retrieval |
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
- Do not infer that `/maketeams` exists from old help text. The current registered entry point is `/scrimmage`.

## Future CS2 / RCON integration

No RCON integration exists on `main` as of this review. If introduced, separate Discord authorization and command parsing from a narrow RCON client, allowlist server actions (e.g. match mode, map change) rather than exposing free-form console commands, keep credentials outside source control, use timeouts, and test against a fake client before any live server.

See [verification](verification.md) for the check matrix and [AGENTS.md](../AGENTS.md) for the working agreement.

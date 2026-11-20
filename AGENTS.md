# ABI LAN Bot — coding-agent entry point

This repository is a .NET 10 Discord bot for LAN events. Treat source code and the current branch as the source of truth: the README may lag behind the project file, and in-flight PRs are not part of `main`.

## First steps

1. Read this file and inspect the files relevant to the task; do not load every document by default.
2. Check the current branch, working tree and open PRs before editing. Keep a change small, reviewable and reversible.
3. Read [architecture and behavior](docs/architecture.md) for code ownership and external side effects, and [verification](docs/verification.md) for the appropriate checks.
4. Use the [Brain](https://rambolarsen.github.io/brain/) as a source of improvement hypotheses, not as a substitute for repository evidence. Its relevant starting points are [documented repository entry points](https://github.com/Rambolarsen/brain/blob/main/practices/documented-repository-entry-points.md), [verification](https://github.com/Rambolarsen/brain/blob/main/concepts/verification.md), and [small reversible changes](https://github.com/Rambolarsen/brain/blob/main/principles/small-reversible-changes.md).

## Stack and build

- Solution: `ABILanBot.sln`; application: `ABILanBot/ABILanBot.csproj`.
- Target framework: `net10.0`. Install a .NET 10 SDK; do not infer the SDK from the older README prose.
- Dependencies are NuGet packages, primarily Discord.Net and Microsoft.Extensions.*.
- From the repository root:

```sh
dotnet restore ABILanBot.sln
dotnet build ABILanBot.sln -c Release --no-restore
```

- There is no automated test project on `main` at the time this guide was written. `dotnet test` returning success without finding tests is **not** evidence of test coverage. Re-check the tree and report exactly what ran.
- Running the bot requires a Discord token and real network access. `Program.cs` loads `appsettings.json` relative to the working directory, so run from `ABILanBot/` rather than assuming `dotnet run --project` from the root will find it.
- Never commit `appsettings.json`, tokens, RCON passwords, IP allowlists containing private infrastructure, or logs containing credentials.

## Where changes belong

- `ABILanBot/Program.cs`: configuration, dependency injection, Discord client startup, event handlers and command registration.
- `ABILanBot/Modules/`: Discord slash-command entry points and user-facing responses.
- `ABILanBot/Services/`: team selection, member/channel operations and HTTP clients.
- `ABILanBot/Entities/`: result/data types.
- `ABILanBot/appsettings.example.json`: public, non-secret configuration examples.

Prefer services for reusable behavior and thin interaction modules. Do not introduce a new framework or major architectural layer for a single command.

## Safety and completion contract

- Treat Discord member moves, channel creation/deletion, guild configuration and future CS2 RCON commands as **live side effects**. Do not exercise them against a real guild/server without explicit authorization; prefer isolated tests and stubs.
- For any new destructive or administrative command, design authorization, scope and failure behavior first. Never accept arbitrary privileged commands from general Discord users. RCON credentials must stay server-side and outside committed configuration.
- Do not silently merge PRs, force-push, delete channels, or alter live infrastructure as part of ordinary development.
- Before proposing completion, describe the change, checks actually executed (pass/fail/not run), residual risks, and how to revert. If blocked by missing SDK, token or access, state that explicitly rather than claiming validation.
- Keep this entry point short. Put subsystem-specific rules next to the subsystem only when recurring needs justify them.

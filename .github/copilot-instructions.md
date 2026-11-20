# Copilot instructions — ABI LAN Bot

Read and follow the repository-wide [AGENTS.md](../AGENTS.md) before implementing changes. For affected behavior, consult [architecture](../docs/architecture.md); for verification and live-operation boundaries, consult [verification](../docs/verification.md). This file is a discoverable pointer, not a second copy of project rules.

Critical constraints even when linked documents are unavailable:

- The actual target is .NET 10 (`ABILanBot/ABILanBot.csproj`); confirm it from the project file rather than stale documentation.
- Work in small reviewable commits/PRs; verify on the current branch rather than assuming pending PR content is merged.
- Do not commit Discord or RCON credentials. Do not invoke live Discord member/channel operations or future CS2 RCON operations without explicit authorization.
- Report which build/tests actually ran; a successful `dotnet test` with zero discovered tests is not evidence of coverage.

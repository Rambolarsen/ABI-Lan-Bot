# Verification guide

A documented command is not proof it has passed. Record the actual command, environment, result and what remains untested. This document intentionally distinguishes compile checks from behavioral tests.

## Prerequisites and local checks

Install .NET 10 SDK. From a clean checkout at the repository root:

```sh
dotnet --version
dotnet restore ABILanBot.sln
dotnet build ABILanBot.sln -c Release --no-restore
```

The source project targets `net10.0`. At the time this document was added, `main` has **no unit/integration test project**, so do **not** claim `dotnet test` provides functional coverage until tests are added and discovered. For a code-only change, the build checks compilation; for a docs-only change, additionally check links and referenced file paths. Neither proves Discord behavior.

A separate pending PR (#7 as of 2026-11-20) proposes a GitHub Actions restore/build workflow. Until it is merged and its checks are observed, do not describe `main` as having enforced CI. Even after merging, build-only CI is not unit-test or integration-test evidence.

## Select checks based on what changed

| Change | Minimum evidence | Additional evidence |
| --- | --- | --- |
| Markdown, agent instructions | Verify local paths/links and consistency with current source | Fresh-session agent can locate build and risk guidance |
| NuGet, csproj, DI, startup | Restore + Release build | Clean checkout and Discord startup in an approved test guild |
| Team allocation or pure logic | Release build | Automated deterministic unit tests once a test project exists |
| Discord interaction/module | Release build | Explicitly authorized sandbox guild: command registration, permissions, error and happy paths |
| Voice moves or channel cleanup | Release build | Fake/mocked operation first; approved test guild with members/channels created for the test |
| Future CS2 RCON | Release build | Fake RCON client, command allowlist and auth tests; approved non-production server only |

## Live execution is opt-in

The bot is a live Discord client, not a harmless local CLI:

1. Create `ABILanBot/appsettings.json` from the example without committing the populated file.
2. Run from the `ABILanBot/` directory (`dotnet run`) **only** when there is explicit authorization and a test guild, because startup connects to Discord and registers commands to the hard-coded guild in `Program.cs`.
3. Do not use a production Discord guild or CS2 server as an automated test environment. In particular, do not test forced channel deletion or RCON commands against real users.
4. Document the guild/server scope used, observable results (without secrets), cleanup, and any limitations.

## Reporting and next gap

In PRs, include: changed behavior, exact validation commands, pass/fail/not-run and reason, risks/side effects, and rollback. Avoid ambiguous claims like “tests pass” when only build ran.

Highest-value follow-up after this entry-point change: add a focused automated test project and extract testable, Discord-independent team-selection logic. That is a separate, small, reversible improvement; do not pretend this documentation PR solves verification maturity by itself.

Sources for the approach: [Brain verification](https://github.com/Rambolarsen/brain/blob/main/concepts/verification.md), [documented repository entry points](https://github.com/Rambolarsen/brain/blob/main/practices/documented-repository-entry-points.md), and [small reversible changes](https://github.com/Rambolarsen/brain/blob/main/principles/small-reversible-changes.md).

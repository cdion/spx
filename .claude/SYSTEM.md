# Spx Always-On Rules

## Discipline
- After any substantive edit, run the narrowest test that can falsify the change.
- Never read `bin/`, `obj/`, `wwwroot/lib/`, `deploy/`, `.github/`, `coverage/`, `artifacts/`.
- Skip `*.g.cs`, `*.g.i.cs`, `GlobalUsings.g.cs`, `AssemblyInfo.cs`.
- Use `path` to scope `find`/`grep`/`ls` by subdirectory. Use `offset`/`limit` on `read` for large files.

## Build Enforcement
- `AnalysisMode=Recommended` + `EnforceCodeStyleInBuild=true` — CA/IDE violations are **build errors**.
- Handlers that log **must** be `internal sealed partial class` with `[LoggerMessage]` — no direct `ILogger.Log*` calls.
- Exceptions only for configuration failures, infrastructure faults, and violated invariants.

## Testing
- Default to unit tests. Integration tests only for EF Core, ASP.NET Identity, endpoint binding, redirects, Orleans runtime.
- NSubstitute 5.3.0 for mocking. Add `using NSubstitute;` when using `Substitute` or `Arg`.
- Every feature or bug fix must include tests. New features need at least one passing test exercising the change. Bug fixes need a regression test that fails before the fix.

## Code Style
- Format with: `dotnet csharpier format <path>`
- Pre-commit hook runs `dotnet build -warnaserror` across solution.

---
description: 'Use after any implementation to run the correct quality gates before finishing. Applies to all files in the repo.'
name: 'Spx Post-Implementation Verification'
applyTo: '**'
---

# Post-Implementation Verification

After any substantive code change, run these gates in order before declaring the task done.

## Gate 1 — Build (catches Roslyn CA errors and code style)

```sh
dotnet build <affected-project>.csproj
```

Use the narrowest project that covers the change. Use `Spx.slnx` only when changes cross project boundaries.

`Directory.Build.props` sets `AnalysisMode=Recommended` and `EnforceCodeStyleInBuild=true` — CA and IDE violations are **build errors**. Zero warnings is the bar.

## Gate 2 — Tests (narrowest project that can falsify the change)

```sh
dotnet test tests/<Affected>.Tests/<Affected>.Tests.csproj --no-build
```

Use the matching test project; only escalate to a full solution test run when the change touches a cross-cutting concern. See `spx-testing.instructions.md` for the project map.

## Gate 3 — Formatting (CSharpier)

CSharpier is enforced by the pre-commit hook. Do **not** attempt to manually reformat code; run the formatter if you added new files or made substantial structural edits:

```sh
dotnet csharpier format <path>
```

## What counts as "done"

A task is only complete when:
1. `dotnet build` exits 0 with 0 warnings on the affected project.
2. All tests in the affected test project pass.
3. No unformatted files (the pre-commit hook will reject them).

Never skip Gate 1 and Gate 2. Gate 3 is handled by the pre-commit hook but run it proactively if you created or heavily restructured `.cs` or `.razor` files.

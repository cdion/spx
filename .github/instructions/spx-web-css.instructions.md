---
description: 'Use when writing or editing Blazor components, adding CSS classes, styling game UI, or deciding between named utilities and inline Tailwind classes in Spx.Web.'
name: 'Spx Web CSS and Tailwind'
applyTo:
  - 'src/Spx.Web/Styles/**'
  - 'src/Spx.Web/Components/**/*.razor'
  - 'src/Spx.Web/Components/**/*.css'
  - 'src/Spx.Web/Components/**/*.cs'
---

# Spx Web CSS and Tailwind

- Tailwind v4, CSS-first config. No `tailwind.config.*` file exists or should be created.
- Source: `src/Spx.Web/Styles/app.css`. Compiled output: `src/Spx.Web/wwwroot/app.css`. Never edit the compiled output directly.
- After editing `app.css`, run the `tailwind: build css` VS Code task to recompile.
- Extend Tailwind via `@theme` in `app.css`. Do not use `tailwind.config.*`.

## Content Scanning

- Tailwind v4 auto-scans all non-gitignored, non-binary project files, including `.cs` files.
- `@source "../Components/**/*.razor"` is already registered explicitly. Do not add redundant `@source` lines for Razor or C# files.
- To force-generate classes that never appear as literals (e.g. for a runtime-only palette), use `@source inline("…")` with brace expansion.

## Dynamic Class Names

- Never build Tailwind class names via string interpolation: `$"bg-{color}-500"` produces no detectable token and will be absent from the compiled CSS.
- For C# color/variant lookups, use a `switch` expression with **complete literal strings** as arms. Tailwind's plain-text scanner detects them.

```csharp
// Correct — each arm is a complete, detectable literal
GameResourceColor.Red  => "border-red-500/40 bg-red-900/20 text-red-100",
GameResourceColor.Blue => "border-sky-500/40 bg-sky-900/20 text-sky-100",
```

- Tone-based component variants (see `StatusBadge.razor`, `AlertBanner.razor`, `SectionHeader.razor`) use a C# `switch` on a `Tone` or `AlertTone` enum to select a complete class string. This is the established pattern — follow it for new tinted components.

## Named vs. Inline Classes

- Add a named `ui-*` class to `@layer components` in `app.css` when the same set of utilities appears in more than one component, or when an `@apply` chain is long enough to obscure intent in a Razor file.
- Use inline Tailwind utilities in Razor for one-off layout, spacing, and responsive adjustments.
- Never use `@apply` outside of `@layer components` blocks in `app.css`.

## Naming Convention

- All named classes use the `ui-` prefix, following `ui-{scope}-{variant}`: `ui-surface`, `ui-panel-card`, `ui-button-primary`.
- Game-specific card classes use `ui-game-card` as the base name.

## Arbitrary Values

- **Acceptable**: complex layout fractions (`grid-cols-[1.3fr_0.9fr]`), background gradients (`bg-[radial-gradient(…)]`), and one-off pixel sentinels (`h-px`). These do not belong in named classes because they are structural choices unique to a single layout.
- **Not acceptable as inline arbitrary**: letter-spacing and font-size overrides. Use `letter-spacing:` as a raw CSS property inside a named `@layer components` class (see `ui-panel-label`, `ui-status-badge`). Prefer Tailwind's built-in `text-xs`/`text-sm` scale before reaching for `text-[0.7rem]`.
- When an arbitrary value is used more than once, move it to a named class or a `@theme` token.

## Component-Scoped CSS Files

- `Components/Layout/ReconnectModal.razor.css` uses a collocated CSS file with hard-coded colors and keyframe animations. This is intentional: the reconnect modal is Blazor framework UI whose class names are owned by the runtime, not by the app. Do not apply this pattern to application components — use Tailwind classes inline or in `app.css` instead.

## Color and Tone Convention

- Use the opacity-suffix tint pattern for semantic states: `border-{color}-400/30 bg-{color}-500/10 text-{color}-200`.
- Tone palette (matches `StatusBadge` and `AlertBanner`):
  - Success → `emerald`
  - Info → `sky`
  - Warning → `amber`
  - Danger → `red` / `rose`
  - Offline / neutral → `slate`
- Resource-color mapping (use consistently for game resource UI):
  - Red → `red-*`
  - Yellow → `amber-*`
  - Blue → `sky-*`
  - Purple → `violet-*`
  - Green → `emerald-*`
  - Orange → `orange-*`

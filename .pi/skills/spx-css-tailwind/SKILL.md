---
name: spx-css-tailwind
description: 'Apply Spx Tailwind v4 conventions — color palette, dynamic classes, @apply rules, and arbitrary values. Use when adding CSS to Blazor components or editing app.css. Trigger words: Tailwind, app.css, ui- class, @apply, dynamic class, resource color, tone palette, arbitrary value, tailwindcss, color palette.'
argument-hint: 'Describe the UI element and its visual treatment.'
---

# Spx CSS & Tailwind

## Config

- Tailwind v4, CSS-first config. No `tailwind.config.*` file exists or should be created.
- Source: `src/Spx.Web.Components/Styles/app.css`. Compiled: `src/Spx.Web.Components/wwwroot/app.css`. Never edit compiled output.
- After editing `app.css`, recompile: `./tools/tailwind/bin/tailwindcss-linux-x64 -i src/Spx.Web.Components/Styles/app.css -o src/Spx.Web.Components/wwwroot/app.css --minify`
- Extend Tailwind via `@theme` in `app.css`. `@source "../Components/**/*.razor"` is already registered.

## Dynamic class names

**Never build class names via string interpolation** — `$"bg-{color}-500"` produces no detectable token and will be absent from compiled CSS.

Use a `switch` expression with complete literal strings:

```csharp
// Correct — each arm is a complete, detectable literal
GameResourceColor.Red  => "border-red-500/40 bg-red-900/20 text-red-100",
GameResourceColor.Blue => "border-sky-500/40 bg-sky-900/20 text-sky-100",
```

## Named vs Inline

- Named `ui-*` class in `@layer components` when the same utility set repeats across components, or an `@apply` chain is long enough to obscure intent.
- Inline Tailwind utilities in Razor for one-off layout, spacing, responsive adjustments.
- Never `@apply` outside `@layer components`.
- Naming: `ui-{scope}-{variant}` — `ui-surface`, `ui-panel-card`, `ui-button-primary`.

## Arbitrary values

- **Acceptable**: complex layout fractions (`grid-cols-[1.3fr_0.9fr]`), background gradients, one-off pixel sentinels (`h-px`).
- **Not acceptable**: letter-spacing and font-size overrides. Use raw CSS inside a named class or Tailwind's `text-xs`/`text-sm` scale.
- When an arbitrary value appears more than once, move to a named class or `@theme` token.

## Color & Tone Palette

Tone palette (StatusBadge, AlertBanner):
- Success → `emerald`
- Info → `sky`
- Warning → `amber`
- Danger → `red` / `rose`
- Offline / neutral → `slate`

Resource colors (game UI):
- Red → `red-*`, Yellow → `amber-*`, Blue → `sky-*`
- Purple → `violet-*`, Green → `emerald-*`, Orange → `orange-*`

Use opacity-suffix tint pattern: `border-{color}-400/30 bg-{color}-500/10 text-{color}-200`.

## Component-scoped CSS

`Components/Layout/ReconnectModal.razor.css` uses collocated CSS with hard-coded colors and keyframes — intentional because Blazor framework UI class names are runtime-owned. Do not apply this pattern to application components.

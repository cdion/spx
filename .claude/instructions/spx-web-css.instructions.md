---
description: 'Use when writing or editing Blazor components, adding CSS classes, styling game UI, or deciding between named utilities and inline Tailwind classes in Spx.Web.'
name: 'Spx Web CSS and Tailwind'
applyTo:
  - 'src/Spx.Web/Components/**/*.razor'
  - 'src/Spx.Web/Components/**/*.css'
  - 'src/Spx.Web/Components/**/*.cs'
  - 'src/Spx.Web.Components/Styles/**'
  - 'src/Spx.Web.Components/Components/**/*.razor'
  - 'src/Spx.Web.Components/Components/**/*.css'
  - 'src/Spx.Web.Components/Components/**/*.cs'
  - 'src/Spx.Web.Playground/Components/**/*.razor'
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

### Choosing a name

| Scope prefix | Purpose | Examples |
|---|---|---|
| `ui-app-*` | Shell chrome — header, nav, brand, main content area | `ui-app-header`, `ui-app-content`, `ui-app-shell` |
| `ui-page-*` | Page-level layout containers and typography | `ui-page`, `ui-page-title`, `ui-page-copy`, `ui-page-header` |
| `ui-surface-*` | Large background containers (forms, sections, empty states) | `ui-surface-form`, `ui-surface-section`, `ui-surface-empty` |
| `ui-panel-*` | Card, row, scrollable sub-containers | `ui-panel-card`, `ui-panel-row`, `ui-panel-label`, `ui-panel-separator` |
| `ui-button-*` | All button variants | `ui-button-primary`, `ui-button-secondary`, `ui-button-icon`, `ui-button-control`, `ui-button-row-*` |
| `ui-type-*` | Typography treatments | `ui-type-overline`, `ui-type-body`, `ui-type-meta`, `ui-type-muted` |
| `ui-badge-*` | Badge/pill treatments | `ui-badge-pill`, `ui-hits-pill` |
| `ui-status-badge` | Notification/status badge | `ui-status-badge` |
| `ui-alert-*` | Banners | `ui-alert`, `ui-alert-error`, `ui-alert-success`, `ui-alert-warning` |
| `ui-tab-*` | Tab buttons | `ui-tab-button-lg`, `ui-tab-button-lg-active` |
| `ui-nav-*` | Navigation pills | `ui-nav-pill`, `ui-nav-pill-danger` |
| `ui-input*` | Form controls | `ui-input`, `ui-select`, `ui-textarea` |
| `ui-*` (misc) | Layout helpers | `ui-flow`, `ui-field`, `ui-label`, `ui-actions`, `ui-empty-state`, `ui-scroll-panel`, `ui-inline-links` |
| `ui-game-card*` | Game card component and color variants | `ui-game-card`, `ui-game-card-action`, `ui-game-card-red`, etc. |
| `ui-pg-*` | Playground-only story UI | `ui-pg-tab`, `ui-pg-ghost-btn`, `ui-pg-nav-link` |
| `ui-balance-*` | JS-interop generated heatmap / balance report | `ui-balance-heatmap-grid`, `ui-balance-bar-fill-attacker` |

## Page layout system

Pages follow a consistent layout hierarchy:

```
ui-app-shell
  └─ ui-app-header (sticky)
  └─ ui-app-main
       └─ ui-page max-w-{2xl|5xl|6xl|7xl}  (page width variant)
            └─ PageHero / SectionHeader
            └─ ui-surface-form / ui-surface-section
                 └─ ui-panel-card / ui-flow / ui-actions
```

Standard page widths:
- `ui-page max-w-2xl` — narrow forms (login, register, etc. — **implicit**, this is the base `ui-page` class)
- `ui-page max-w-5xl` — lobby home page
- `ui-page max-w-6xl` — game lobby, nexus game page
- `ui-page max-w-7xl` — balance report

The `ui-page` class includes `mx-auto flex max-w-2xl flex-col gap-8` by default. When overriding width, write `ui-page max-w-6xl gap-6` (keep the `ui-page` base, add width + gap overrides).

```razor
@* Narrow form *@
<section class="ui-page">...</section>

@* Lobby page *@
<section class="ui-page max-w-5xl">...</section>

@* Game play page *@
<section class="ui-page max-w-6xl gap-6 lg:gap-7">...</section>
```

## Responsive sidebar / grid layout patterns

The codebase uses several recurring responsive grid patterns. Use arbitrary-value grid columns for sidebar/content splits; do **not** extract into named classes since the fractions vary per layout.

### Main + sidebar (lobby pages, story pages)

```razor
<div class="grid gap-6 lg:grid-cols-[1.3fr_0.9fr]">  @* wider main, narrower sidebar *@
    <section class="ui-surface-form">...</section>
    <aside class="ui-surface-form">...</aside>
</div>
```

### Main + sidebar (forms/stories)

```razor
<div class="mb-6 grid gap-3 lg:grid-cols-[1.45fr_1fr]">  @* 45% wider main *@
    <div class="ui-panel-card">...</div>
    <div class="ui-panel-card">...</div>
</div>
```

### Flex ratio layout (gameplay panel)

```razor
<div class="flex min-h-0 flex-col gap-4 xl:flex-row">
    <section class="ui-surface-section flex min-h-0 min-w-0 flex-col p-4 lg:p-5 xl:flex-[3_1_0%]">  @* hex grid: 60% *@
    </section>
    <aside class="flex min-h-0 min-w-0 flex-col gap-4 xl:flex-[2_1_0%]">  @* sidebar: 40% *@
    </aside>
</div>
```

### 3-column grid (info bar)

```razor
<div class="grid gap-3 sm:grid-cols-3">
    <article class="ui-panel-row">...</article>
    <article class="ui-panel-row items-center justify-center">...</article>
    <article class="ui-panel-row">...</article>
</div>
```

### Timeline + message composer

```razor
<div class="grid gap-4 lg:grid-cols-[1.35fr_0.85fr] lg:gap-5">
    <NexusTimelinePanel ... />
    <LobbyMessageComposer ... />
</div>
```

## UI surface hierarchy

The `ui-surface-*` classes form a family of background containers styled for different use cases. Pick the right one:

| Class | Use when |
|---|---|
| `ui-surface-form` | Large forms, central page content that needs emphasis (login, register, lobby quick start) |
| `ui-surface-form-tight` | Same look but tighter padding (`p-5 sm:p-6 lg:p-7` vs `p-6 sm:p-8`) |
| `ui-surface-empty` | Loading states, unavailable-game fallback (single container, no interactions) |
| `ui-surface-section` | Reusable section within a larger view (gameplay panel, selected hex panel sidebar). Designed to be nested inside flex layouts with `flex min-h-0 flex-col`. |

Use `SurfacePanelShell` component (from `Spx.Web.Components`) when you need a configurable surface with optional header and body regions:

```razor
<SurfacePanelShell SurfaceClass="ui-surface-form-tight space-y-5" BodyClass="space-y-4">
    <HeaderContent>...</HeaderContent>
    <ChildContent>...</ChildContent>
</SurfacePanelShell>
```

## Transition & interaction conventions

Every interactive element (button, link, clickable row) uses `transition-colors` for consistent hover/focus feedback. The pattern:

```css
/* Standard interactive state transitions */
transition-colors                      /* default for all interactive elements */
transition-colors duration-150         /* used on <a> base rule */
```

Hover states follow an opacity-escalation pattern — start subtle, brighten on hover:
- Links: `text-sky-300` → `hover:text-sky-200`
- Secondary buttons: `border-white/10` → `hover:border-sky-300/60` and `text-slate-200` → `hover:text-white`
- Icon buttons: `border-white/10 bg-slate-900/80` → `hover:border-white/20 hover:bg-white/5`
- Disabled: All interactive elements use `disabled:cursor-not-allowed` with muted colors (`text-slate-500`, `opacity-50`)

Do not add custom transitions or animations outside of the app.css base rules. If a component needs a unique transition or keyframe (e.g., Blazor render-framework elements), use a collocated `.razor.css` file.

## Domain-specific color switching

Components that render player-specific data use a **faction-based dynamic color pattern**. Instead of hardcoding an allegiance, compute the class via the same switch each time:

```csharp
// Standard pattern — faction color drives the accent
CurrentPlayerFaction == NexusFactionColor.Red ? "text-red-300" : "text-sky-300"
CurrentPlayerFaction == NexusFactionColor.Red ? "text-red-400" : "text-sky-400"
CurrentPlayerFaction == NexusFactionColor.Red ? "ui-button-row-active-danger" : "ui-button-row-active-info"
```

This pattern is used in:
- **`NexusSelectedHexPanel`** — `MyForcesClass`, `EnemyForcesClass`, `GetFleetRowClass`, `GetStackNameClass`, `GetBuildRowClass`, `GetSystemLabelClass`
- **`NexusGameplayTopInfoBar`** — `PlayerNameClass`
- **`NexusSelectedHexPanel`** — `SelectionCountClass`, `GateButtonClass`

When a component needs to render opponent colors, compute the opposite:

```csharp
private NexusFactionColor OpponentFactionColor =>
    CurrentPlayerFaction == NexusFactionColor.Red ? NexusFactionColor.Blue : NexusFactionColor.Red;
```

## Arbitrary values

- **Acceptable**: complex layout fractions (`grid-cols-[1.3fr_0.9fr]`), background gradients, one-off pixel sentinels (`h-px`), flex ratios (`xl:flex-[3_1_0%]`).
- **Not acceptable**: letter-spacing and font-size overrides. Use raw CSS inside a named class or Tailwind's `text-xs`/`text-sm` scale.
- When an arbitrary value appears more than once, move to a named class or `@theme` token.

## Letter-spacing

Letter-spacing is used **only inside named `ui-*` classes** via raw CSS in `@apply` chains. Never use inline `style="letter-spacing: ..."` — if you need letter-spacing, add it to an existing named class or create one.

Named classes that include letter-spacing:

| Class | Value | Used for |
|---|---|---|
| `ui-type-overline` | `0.2em` | Section/subsection overline labels |
| `ui-app-brand-link` | `0.2em` | Brand text in header |
| `ui-app-version` | `tracking-widest` | Version string in header |
| `ui-section-eyebrow` | `0.25em` | Eyebrow text above section titles |
| `ui-panel-label` | `0.2em` | Panel section labels |
| `ui-code-token` | `0.2em` | Invite codes, game tokens |
| `ui-input-code` | `0.35em` | Invite code input field |
| `ui-status-badge` | `0.2em` | Status badges |
| `ui-inline-badge` | `0.2em` | Inline badges |
| `ui-button-*` | `0.01em` or `0.08em` | All button variants |

## Inline SVGs

Use inline SVGs for simple icons (checkmark for copied state, star icon for home system, close/delete icons). Keep SVGs small, direct, and accessible:

```razor
@* ✅ inline SVG with aria-hidden and focusable="false" *@
<svg viewBox="-5 -5 10 10" class="h-3.5 w-3.5 fill-white/85" aria-hidden="true" focusable="false">
    <polygon points="0,-4 1.1,-1.5 3.8,-1.2 1.8,0.8 2.4,3.6 0,2.2 -2.4,3.6 -1.8,0.8 -3.8,-1.2 -1.1,-1.5" />
</svg>
```

Guidelines:
- Always set `aria-hidden="true"` and `focusable="false"` on decorative SVGs
- Place SVGs inside `<button>` or other interactive elements with a textual `title` or `aria-label`
- Use `fill-current` or explicit fill colors from the palette (not hardcoded hex except for pure white)
- For reusable icons across multiple components, consider a shared partial or component

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

### Combat hits badge

Hits pills use a 3-tier severity pattern reflecting remaining hit points:

```csharp
// Full health
stack.RemainingHits >= stack.UnitType.Profile().Hits
    => "border-emerald-400/30 bg-emerald-500/10 text-emerald-200"
// Damaged
stack.RemainingHits > 1
    => "border-amber-400/30 bg-amber-500/10 text-amber-200"
// Critical
_ => "border-rose-400/30 bg-rose-500/10 text-rose-200"
```

## Named classes for JS-generated elements

When JavaScript generates DOM elements (e.g., the balance report heatmap), use a dedicated `ui-balance-*` namespace to avoid class-name collisions and signal that the elements are not Razor-rendered:

```css
.ui-balance-heatmap-cell { ... }
.ui-balance-heatmap-cell.is-selected { ... }
.ui-balance-heatmap-value { ... }
```

These classes live in `app.css` and use the `.is-{state}` modifier pattern (not Tailwind utilities) because the JS dynamically adds/removes them.

## Component-scoped CSS

`Components/Layout/ReconnectModal.razor.css` uses collocated CSS with hard-coded colors and keyframes — intentional because Blazor framework UI class names are runtime-owned. Do not apply this pattern to application components.

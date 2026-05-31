# Nexus Protocol — Game Spec

## Overview

1v1 abstract space-themed 4X game. Two factions on a hex grid race to construct a Nexus Gate at the center system.

**Format:** 1v1 · Hex grid · Fully visible · Simultaneous turns  
**Target length:** 20–30 minutes  
**4X:** Expand across systems, Exploit resource income, Exterminate opposing forces, race to the Nexus

---

## Map — 19 Systems

```
         [ ]  [ ]  [ ]
       [ ]  [ ]  [ ]  [ ]
     [ ]  [ ]  [N]  [ ]  [ ]
       [ ]  [ ]  [ ]  [ ]
         [ ]  [ ]  [ ]
```

- **Nexus** `[N]` — center system; win site only, no income, cannot be controlled for income
- **Home systems** — two opposing outer systems, one per player; each produces **2 Energy/turn**; under that player's control from game start
- **16 income systems** — every remaining system; each is assigned a random income value of **1–3 Energy/turn** at game creation; values are fixed for the duration of the game
- Income values are placed asymmetrically — no symmetry guarantee between the two sides
- The income value of every system is visible to both players from turn 1
- Map is fully visible from turn 1

---

## Resources — Energy

One resource type: **Energy**.

| Source | Income |
|---|---|
| Home system | +2 Energy/turn |
| Income system | +1–3 Energy/turn (value assigned at map generation) |

- Controlling a system earns its Energy income; the opponent loses that income if they previously controlled it
- Energy has no upper limit; unused Energy carries over between turns
- Each player begins with **0 Energy**

---

## Supply

Each player has a **supply pool** equal to the total income of all systems they currently control. Supply determines how many Capital ships a player can sustain.

- **Only Capital ships** (Frigate, Destroyer, Cruiser, Carrier) draw against supply. Strike craft and planetary units are always free to hold.
- **The Nexus** has `IncomeValue = 0` and contributes nothing to supply even when controlled. Every Capital stationed there is unsupported.
- **Uncontrolled and contested systems** contribute 0 supply, regardless of units present there.

### Supply Check — automatic disbanding

After newly built units deploy each round, if a player's total Capital count exceeds their supply pool, the excess Capitals are automatically disbanded. Disbanding evaluates systems in **spiral order** — Nexus first, then Ring 1 clockwise from NE (Alpha → Zeta), then Ring 2 clockwise from NE (Eta → home systems). Within each system, the cheapest-build-cost Capital is disbanded first; ties are broken by most damage absorbed (weakest unit first). Disbanding continues until Capitals ≤ supply.

**Strategic consequences:**
- Capitals pushed forward toward the Nexus are the first lost when supply runs short
- Losing controlled systems in combat shrinks the supply pool immediately — a good attack can collapse the opponent's fleet *and* their economy in the same round
- Committing energy to the Nexus Gate (rather than new Capitals) leaves the existing fleet vulnerable if income drops
- Home systems are the last place disbanding reaches; home-fleet Capitals are the safest to hold

---

## Units

Three categories of units: **Capital**, **Strike**, and **Planetary**.

**Movement:**
- A Move order selects any subset of units on a system and an adjacent destination system; all selected units move together
- Ships can always be included in a move; they provide carry capacity (Carrier: 8, Cruiser: 2; Frigates and Destroyers: 0)
- Strike craft and planetary units may only move if included alongside capital ships whose combined capacity covers them; each strike or planetary unit consumes 1 capacity slot
- Planetary units that have committed to a contested system are locked in place until that system is no longer contested
- A system may contain any number of units from either player; units not included in a move order stay in place
- Multiple separate Move orders may originate from the same system in the same turn (e.g. one set of units moves, another stays)

**Building:** All unit types are built at the home system. Any number of units may be built per turn, limited only by available Energy.

### Capital

| Unit | Category | Cost | Hull | Silhouette | Base hit | Combat role |
|---|---|---|---|---|---|---|
| **Frigate** | Capital | 4 | 2 | 2 | 4+ | Anti-ship; weak against strike craft |
| **Destroyer** | Capital | 5 | 2 | 2 | 4+ | Anti-strike and anti-ship; participates in Screen and Engage |
| **Cruiser** | Capital | 6 | 2 | 3 | 3+ | Heavy anti-capital with bombard support; needs escort against strike craft; capacity 2 (any mix of strike craft and planetary units) |
| **Carrier** | Capital | 8 | 4 | 4 | 6+ | Transport; capacity 8 (any mix of strike craft and planetary units) |

### Strike

| Unit | Category | Cost | Hull | Silhouette | Base hit | Combat role |
|---|---|---|---|---|---|---|
| **Interceptor** | Strike | 2 | 1 | 1 | 4+ | Counters fighters and bombers across Screen and Engage; cannot attack ships |
| **Fighter** | Strike | 2 | 1 | 1 | 4+ | General-purpose; engages all enemy strike craft |
| **Bomber** | Strike | 4 | 1 | 2 | 5+ | Attacks capital ships in Engage; bombards planetary units in Bombard |

### Planetary

| Unit | Category | Cost | Hull | Silhouette | Base hit | Combat role |
|---|---|---|---|---|---|---|
| **Infantry** | Planetary | 2 | 1 | 1 | 4+ | General ground combat |
| **Armor** | Planetary | 4 | 2 | 2 | 3+ | Armored assault; advantage over infantry |

Only planetary units may begin **Nexus Gate** construction.

**Starting composition:** 1 Carrier, 4 Infantry, 2 Fighters — placed at the player's home system at game start.

---

## Round Structure

Each round follows this sequence:

1. **Plan phase** — both players simultaneously and secretly commit orders
2. **Resolve phase:**
   1. Build cost deducted · Nexus Gate payment deducted
  2. Moves — all units move simultaneously. Units whose paths cross in opposite directions (A moves to B's system while B moves to A's system) simply swap — no combat at either system. If units arrive at a system occupied by enemy units, they stop and combat resolves in step 3. Planetary units arriving at an opponent-controlled or uncontrolled system with no enemy units present can establish control immediately.
  3. Combat — all contested systems resolve in spiral order (Nexus → Ring 1 → Ring 2, homes last); each system runs Screen, Engage, commitment, Bombard, and Assault before the next system resolves
   4. Income — all income calculated and applied simultaneously; a player earns income from every system they control
   5. Newly built units appear at home system
   6. Supply check — if a player's Capital count exceeds their supply pool, excess Capitals are automatically disbanded in spiral order (see Supply section)
3. **Win check**

---

## Orders

Each unit may be given one order per turn. A unit with no order stays in place.

| Order | Effect |
|---|---|
| **Move** | Select any movable units on this system and an adjacent destination system; move them together. Capital ships in the selection provide capacity; each strike craft or planetary unit in the selection consumes 1 capacity slot. A selection with strike craft or planetary units but insufficient capital ship capacity is invalid. Planetary units already committed to a contested system are locked and cannot be selected. Combat resolves at the destination if the opponent has units there. |

Multiple units on the same system may each be assigned different orders independently.

Player-level orders (not unit-specific):

| Order | Effect |
|---|---|
| **Build [unit]** | At home system only; costs vary by unit type (see Units section); any number of units may be built per turn; units appear at end of Resolve and can receive orders the following turn |
| **Begin Nexus Gate** | At least one planetary unit must be present on an uncontested Nexus; commits resources toward construction |

---

## System Control

A player **controls** a system when they have at least one planetary unit in it and the opponent has no units there. Control is the mechanism for earning income from a system.

**Establishing control:** Planetary units arriving at an uncontrolled system or an opponent-controlled system with no enemy units present take control of it immediately during the Moves step. No order is required — presence is sufficient.

**Retaining control:** A player retains control of a system after voluntarily moving their planetary units away. The system stays controlled until enemy units arrive and contest it.

**Contested system:** A system with units from both players present is contested — neither player controls it and it produces no income for either player. After Engage, all surviving planetary units in that system commit to the fight. Committed planetary units stay locked in the system while it remains contested, can be bombarded, and participate in Assault. Once the system is no longer contested, surviving committed planetary units return to the fleet. If all planetary units on both sides are eliminated in combat, the system becomes uncontrolled.

**Capital ships and strike craft** cannot establish control on their own. They can, however, contest an existing controller while they remain in the system.

**Uncontrolled system:** A system that has never been captured, or where combat eliminated all planetary units from both sides. Produces no income until one player's planetary units arrive.

**Home systems** are under each player's control from game start and follow the same rules as any other system — they can be captured if the opponent's planetary units arrive uncontested.

**The Nexus** cannot be controlled for income regardless of planetary unit presence.

---

## Combat

When units from both players occupy the same system after moves resolve, combat resolves in four sequential **exchange phases** with a planetary commitment step after Engage. If multiple systems are contested, they resolve in **spiral order** (Nexus first, then Ring 1 clockwise from NE, then Ring 2 clockwise from NE, with home systems last). Each system's combat completes fully before the next contested system begins. Each phase is an attrition exchange — both sides roll simultaneously, casualties are applied after all dice resolve, and survivors carry forward. Neither side retreats; both may remain on a contested system after all phases complete.

### Dice System

Each unit rolls 1d6 in each phase it participates in. A result at or above the unit's **effective hit threshold** scores 1 hit on an enemy unit. Each hit randomly targets one enemy unit; the probability of a unit being selected is proportional to its **silhouette** (targeting weight). A unit is destroyed when it has absorbed hits equal to its **hull** (HP). Silhouette and hull are currently equal for all units and will diverge in a future balance pass. Units fight at full strength until destroyed. A threshold below 2 always hits; a threshold above 6 never hits.

### Full Interaction Matrix

**P1** = Screen · **P2** = Engage · **P3** = Bombard · **P4** = Assault · **—** = cannot target this category

| Attacker | vs Strike | vs Capital | vs Planetary |
|---|---|---|---|
| **Fighter** | 4+ (P1) | 6+ (P2) | — |
| **Interceptor** | 2+ vs bomber · 4+ vs fighter (P1, P2) · 3+ vs interceptor (P1, P2) | — | — |
| **Bomber** | 5+ vs fighter/bomber · 6+ vs interceptor (P1) | 4+ (P2) | 4+ (P3) |
| **Destroyer** | 4+ (P1) | 5+ vs strike · 4+ vs capital (P2) | — |
| **Frigate** | 5+ (P2) | 4+ (P2) | — |
| **Cruiser** | 6+ (P2) | 3+ (P2) | 4+ (P3) |
| **Carrier** | 6+ (P2) | 6+ (P2) | — |
| **Infantry** | — | — | 4+ vs inf · 5+ vs armor (P4) |
| **Armor** | — | — | 3+ vs inf · 4+ vs armor (P4) |

† Fighter vs Ship (P2): 6+ — fighters can technically harass ships but are nearly ineffective against them.  
§ Infantry: 4+ vs infantry, 5+ vs armor. Armor: 3+ vs infantry, 4+ vs armor.

### Phase Participation

**A** = attacks (rolls dice) · **T** = targetable (can receive hits) · **—** = not present this phase

| Unit | P1 Screen | P2 Engage | P3 Bombard | P4 Assault |
|---|---|---|---|---|
| **Interceptor** | A · T | A · T | — | — |
| **Fighter** | A · T | A · T | — | — |
| **Bomber** | A · T | A · T | A | — |
| **Destroyer** | A only (not targetable) | A · T | — | — |
| **Frigate** | — | A · T | — | — |
| **Cruiser** | — | A · T | A | — |
| **Carrier** | — | A · T | — | — |
| **Infantry** | — | — | T only | A · T |
| **Armor** | — | — | T only | A · T |

Silhouette-weighted random targeting applies only within the eligible target pool for the attacking unit's current phase. A unit cannot be targeted in a phase where it is marked **—**.

### Phase 1 — Screen

Interceptors, fighters, and bombers attack other strike craft. Destroyers attack strike craft but **cannot be targeted** in Screen. Capital ships (Frigate, Cruiser, Carrier) are absent. Skipped if neither side has any eligible units.

### Phase 2 — Engage

All capital ships participate. Surviving strike craft remain present and are targetable, and fighters, interceptors, bombers, and destroyers can attack strike craft. Interceptors still cannot attack capital ships in Engage. Skipped if neither side has any capital ships.

### Commitment Step — After Engage

After Engage resolves, all surviving planetary units in that contested system commit to the fight. Committed planetary units remain visible in the system but are locked and cannot move while the system stays contested.

### Phase 3 — Bombard

Surviving bombers (4+) and cruisers (6+) each roll against enemy committed planetary units. Planetary units cannot return fire. Hits applied before Assault. Skipped if the attacker has no surviving bombers or cruisers, or the defender has no committed planetary units.

### Phase 4 — Assault

Participants: committed infantry and committed armor. Capital ships are not present and cannot be targeted in Assault. Skipped if neither side has eligible committed ground units.

### System Outcome

After all four phases, both players plan orders for their surviving units the following turn.

- **Both sides have surviving units:** system is contested — neither controls it, no income for either
- **One side eliminated:** surviving player holds the system; if they have planetary units present they control it; any committed planetary units then return to the fleet because the system is no longer contested; if only capital ships or strike craft remain, control is unchanged from before combat
- **All planetary units on both sides eliminated:** system becomes uncontrolled regardless of prior state

---

## Win Condition

**Build a Nexus Gate** at the center system:

- At least one of your planetary units must occupy an uncontested Nexus; declare **Begin Nexus Gate**
- Total cost: **24 Energy**, committed over **2 consecutive turns** (12 Energy per turn)
- **Turn N:** declare construction, commit 12 Energy; construction status is visible to both players
- **Turn N+1:** commit 12 Energy; gate completes — you win
- Construction is cancelled and all committed resources are lost if: all planetary units on the Nexus are eliminated in combat; the planetary units move away voluntarily; or the player cannot commit the remaining resources on turn N+1
- The construction check happens after combat resolves — at least one planetary unit must survive combat on the Nexus and the Nexus must still be uncontested for construction to proceed that turn
- If both players complete the Nexus Gate in the same turn: **draw**

---

## Resolve Events

The resolve phase emits a typed sequence of events. The front end consumes these to update board state and render the round log. Events are produced in resolution order; skipped steps (e.g. a combat phase with no eligible units) produce no events. The message rendering in [Resolve Phase Messages](#resolve-phase-messages) derives from this stream.

### Movement

| Event | Fired when | Key data |
|---|---|---|
| `NexusUnitsMovedEvent` | A player's units leave one system and arrive at another | `PlayerId`, `From`, `To`, `Units` (type → count), `IsRetreat` |

`IsRetreat = true` when the source system was contested before moves resolved (the player is moving out of a fight). Retreat moves and normal moves use the same event type.

### System Control

| Event | Fired when | Key data |
|---|---|---|
| `NexusPlanetaryControlEvent` | A player gains or retains sole planetary presence in a system | `System`, `PlayerId` |
| `NexusSystemContestedEvent` | Both players have planetary units in the same system | `System` |
| `NexusSystemUncontrolledEvent` | All planetary units are gone from a system — control cleared | `System` |

### Combat

| Event | Fired when | Key data |
|---|---|---|
| `NexusCombatBeganEvent` | Combat is about to resolve at a system | `System`, `Player1Id`, `Player2Id` |
| `NexusPhaseResultEvent` | One combat phase (Screen/Engage/Bombard/Assault) completes | `System`, `Phase` (1–4), `Losses` (per player/type/count), `AttackRolls` (individual dice) |
| `NexusSystemClearedEvent` | All units of one player are eliminated from a system | `System`, `VictorId` |

Only phases where at least one side has eligible units produce a `NexusPhaseResultEvent`. `AttackRolls` carries every individual die roll (attacker type, target type, roll, threshold, hit/miss) to support detailed log rendering.

### Income & Deployment

| Event | Fired when | Key data |
|---|---|---|
| `NexusIncomeEvent` | A player collects income for the round | `PlayerId`, `Amount`, `Sources` (list of contributing systems) |
| `NexusUnitDeployedEvent` | A newly built unit appears at the player's home system | `PlayerId`, `UnitType`, `HomeSystem`, `Count` |

One `NexusUnitDeployedEvent` fires per unit type per build order; multiple orders of the same type in one turn produce separate events.

### Supply *(pending implementation)*

| Event | Fired when | Key data |
|---|---|---|
| `NexusCapitalDisbandedEvent` | A Capital is removed because it exceeds the player's supply pool | `PlayerId`, `UnitType`, `System`, `Count` |

Events fire in spiral order (Nexus → Ring 1 → Ring 2), one per unit type per system. All disband events for a given round resolve before the Gate check.

### Nexus Gate

| Event | Fired when | Key data |
|---|---|---|
| `NexusGateStartedEvent` | A player commits the first 12 Energy; construction begun | `PlayerId`, `System` |
| `NexusGateCompletedEvent` | A player commits the second 12 Energy; gate complete | `PlayerId`, `System` |
| `NexusGateCancelledEvent` | Construction cancelled — planetary units lost, voluntarily moved, or insufficient energy | `PlayerId`, `System` |

Energy already committed when a gate is cancelled is forfeited.

### Game End

| Event | Fired when | Key data |
|---|---|---|
| `NexusVictoryEvent` | One player completes the Nexus Gate | `WinnerId` |
| `NexusDrawEvent` | Both players complete the Gate on the same turn | `Reason` |

---

## Resolve Phase Messages

One message is appended to the game log for each significant event during resolution. Messages are produced in resolve order.

### Moves

| Event | Message |
|---|---|
| Units move | `[A]'s [unit type] moved from [System] to [System]` |
| Units retreat | `[A]'s [unit type] retreated from [System] to [System]` |
| Planetary units take control | `[A] controls [System]` |

### Combat (one block per contested system, four phases)

| Event | Message |
|---|---|
| Combat begins | `Combat at [System] — [A]: [summary] vs [B]: [summary]` |
| Phase 1 result | `Screen at [System] — [A] loses [N] strike craft, [B] loses [N] strike craft` |
| Phase 2 result | `Engage at [System] — [A] loses [N] ship(s), [B] loses [N] ship(s)` |
| Bombard | `[A]'s bomber(s) strike [System] — [B] loses [N] ground unit(s) to bombard` |
| Phase 4 result | `Assault at [System] — [A] loses [N] ground unit(s), [B] loses [N] ground unit(s)` |
| System cleared (one side eliminated) | `[A] holds [System] — [B] has no surviving units` |
| System contested (both survive) | `[System] remains contested — both players have surviving units` |
| System becomes uncontrolled | `[System] is now uncontrolled` |

### Income

| Event | Message |
|---|---|
| Income received (one per player) | `[A] receives +[N] Energy this turn` (all controlled systems collapsed into one line) |
| Unit deployed (appears end of resolve) | `[A] deployed a new [unit type] at [Home]` |

### Supply Check

| Event | Message |
|---|---|
| Capital disbanded | `[A]'s [unit type] at [System] disbanded — supply exceeded` |

### Win Check

| Event | Message |
|---|---|
| Gate construction begun (turn 1 of 2) | `[A] begins Nexus Gate construction — 12 Energy committed (turn 1 of 2)` |
| Gate construction completes (turn 2 of 2) | `[A] commits final 12 Energy — Nexus Gate complete` |
| Gate construction cancelled | `[A]'s Nexus Gate construction cancelled — committed resources lost` |
| Victory by gate | `[A] wins — Nexus Gate constructed` |
| Draw by simultaneous gate | `Draw — both players completed the Nexus Gate simultaneously` |

---

## Game Start

Games are created via invite link:

1. Player A creates a game — receives a unique invite URL
2. Player A shares the URL with Player B
3. Player B opens the URL and joins — the game begins immediately
4. There is no turn timer; the game is fully async (players plan and submit whenever ready)
5. Each player's submitted orders are revealed and resolved only once **both** players have submitted for that round

---

## UX — Game Screen

## UX — Game Screen

### Wireframe — Planning Phase

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│  Nexus Protocol · Round 4 · PLANNING                [⚙ 1/2]    [Submit Orders]    │
├────────────────────┬────────────────────────────────────┬───────────────────────────┤
│                    │                                    │  Round 4 — Planning       │
│  YOU  (Blue)       │                                    │  ──────────────────────   │
│  ⚡ 12 → 8        │        ·    ·    ·                 │  ○ Combat at S-03         │
│                    │      ·    ·    ·    ·              │    A loses 1 frigate      │
│  OPPONENT  (Red)   │    ·    ·   [N]   ·    ·          │  ○ A controls S-07        │
│  ⚡ 24             │      ·    ·    ·    ·              │  ○ A receives +11 ⚡      │
│                    │        ·    ·    ·                 │                           │
│  ─────────────────│                                    │  Round 3                  │
│                    │                                    │  ──────────────────────   │
│  PENDING ORDERS    │                                    │  ○ S-04 → S-05            │
│                    │                                    │  ○ B controls S-05        │
│  Carrier+2 Inf     │                                    │  ○ Combat at S-05         │
│  S-03 → S-05  [×] │                                    │    B loses 2 infantry     │
│                    │                                    │                           │
│  Build Cruiser [×] │                                    │  ──────────────────────   │
│                    │                                    │  You:   good luck!        │
│                    │                                    │  Them:  you too           │
│                    │                                    │                           │
│                    │                                    │  ──────────────────────   │
│                    │                                    │ ┌───────────────────────┐ │
│                    │                                    │ │ Type a message…     ↵ │ │
│                    │                                    │ └───────────────────────┘ │
└────────────────────┴────────────────────────────────────┴───────────────────────────┘
```

**System display (zoomed — your controlled system, mid-game):**
```
    ╔══════════╗
    ║  Vega    ║   ← sector name (non-home, non-Nexus systems only)
    ║  +3 ⚡   ║   ← income label (income systems; monospace yellow)
    ║    ◆     ║   ← your presence diamond (faction color)
    ╚══════════╝
       (blue tint = you control)
```

When both factions have units on the same system, two diamonds appear side by side — yours offset left, opponent's offset right:
```
    ╔══════════╗
    ║  +3 ⚡   ║
    ║  ◆    ◆  ║   ← your color left · opponent color right
    ╚══════════╝
       (contested — no tint)
```

Nexus system with gate construction in progress (pip 1 of 2 lit):
```
    ╔══════════╗
    ║          ║
    ║   ● ○    ║   ← two pips; filled = completed stage, dim = pending
    ╚══════════╝
```

### Layout

Three-column layout with a persistent top bar:

- **Top bar:** game name · round counter · phase indicator · Nexus Gate construction badge (when active, e.g. ⚙ Turn 1/2; visible to both players) · Submit Orders button (Planning phase only)
- **Left sidebar:** both players' Energy totals — yours on top, opponent below; fully visible at all times (open information); during Planning phase your Energy shows the projected balance after all queued build costs (e.g. “12 → 8”); pending orders list below resources during Planning phase
- **Center:** hex grid — primary interactive area; all orders assigned via grid interaction
- **Right sidebar:** game log and player chat interleaved chronologically; message input pinned at the bottom

### System Display

Each system shows:
- **Background tint / border:** control indicator (your color / opponent color / contested / uncontrolled gray); Nexus uses a distinct neutral style
- **Income label:** Energy income value shown on every income system (e.g. `+3`); home systems show their fixed `+3`; Nexus shows no income
- **Unit presence:** a small colored diamond renders in the lower portion of the hex for each faction that has units there; your faction's diamond is offset left when both are present, opponent's right; no numeric count is shown on the hex
- **Nexus Gate badge:** when your gate construction is in progress, two small pip circles render on the Nexus hex; the first pip is lit in your faction color when stage 1 is complete, the second when stage 2 is complete

### Order Assignment — Planning Phase

Orders are assigned system-first: click a system to open a unit selection panel, configure the move, then click the destination.

**Assigning a Move order:**
1. **Click a system** — if it contains your units, a unit selection panel appears anchored to that system; each unit type is shown with its count and a toggle
2. **Toggle units** in the panel to include them in the selection
3. **Destination systems highlight** on the grid based on the current selection; systems are grayed if capacity is insufficient for the selected strike craft and planetary units
4. **Click a highlighted destination system** — the Move order is created; the panel closes; an arrow overlay appears from source to destination
5. **Click elsewhere or close** — closes the panel without assigning an order

To create a second Move order from the same system (e.g. send some units to system A, others to system B): after the first order is assigned, click the system again — the panel reopens showing only units that have not yet been assigned an order.

**Assigning Begin Nexus Gate:** click the Nexus system while your planetary units are on it — the unit panel shows a "Begin Nexus Gate" action alongside the unit toggles; activate it to queue the order.

**Build [unit]:** click the home system background (not a unit in the panel) — a build panel appears listing available unit types with their costs, greyed out if the projected Energy balance is insufficient; click a unit type to queue it. Multiple builds may be queued in one turn.

### Pending Orders List

During Planning phase, the bottom of the left sidebar shows all orders queued so far this turn:

- **One row per Move order**, showing the unit composition and destination (e.g. `Carrier + 2 Infantry → System 7`); × removes the entire move and returns all units in it to unassigned
- **One row per Build order** (e.g. `Build Carrier`); × cancels it and restores the projected Energy
- **Begin Nexus Gate** appears as its own row when queued; × cancels it
- **Click a row** — reopens the unit panel for that system with the order pre-loaded, allowing reassignment
- List is empty at the start of each Planning phase
- Arrow overlays on the grid and the pending orders list are two views of the same state

### Phase States

| Phase | Grid behaviour | Submitted orders |
|---|---|---|
| Planning | Units are clickable; orders assigned via grid | Arrow overlays on grid + pending orders list in sidebar |
| Waiting | Read-only | Your submitted orders remain visible as ghost arrows |
| Resolving | Read-only; map updates event-by-event in sync with log | Previous orders cleared |

### Resolve Display

Resolution plays out automatically in real time, visible to both players simultaneously. Neither player advances it manually — after all events complete, both enter the next Plan phase together.

Each resolve step animates on the hex grid and appends to the game log with a fixed delay between events:

| Step | Animation |
|---|---|
| Build cost deducted | Resource counters tick down (silent — confirmed at unit appearance) |
| Moves | Unit icons slide to destination systems simultaneously |
| Combat — each phase | Contested system flashes per phase; unit count numbers animate down |
| Control change | System control color transitions |
| Income | Resource counters tick up |
| Unit appears | New unit icon fades in at home system |

**Timing:** Fixed delay — each log entry appears and its grid animation plays before the next event fires.

**Multiple simultaneous combats** are displayed sequentially in ring-inward order: ring-2 hexes first, ring-1 next, Nexus last. This gives a natural narrative feel of conflict sweeping toward the center.



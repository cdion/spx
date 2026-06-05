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
- **16 income systems** — every remaining system; each is assigned a random income value of **1–2 Energy/turn** at game creation; values are fixed for the duration of the game
- Income values are placed asymmetrically — no symmetry guarantee between the two sides
- The income value of every system is visible to both players from turn 1
- Map is fully visible from turn 1

---

## Resources — Energy

One resource type: **Energy**.

| Source | Income |
|---|---|
| Home system | +2 Energy/turn |
| Income system | +1–2 Energy/turn (value assigned at map generation) |

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
| **Frigate** | Capital | 3 | 1 | 2 | 4+ | Anti-ship and anti-strike; provides escort (protects one non-Escort Capital per Frigate); has **shield** (absorbs first hit per turn) |
| **Destroyer** | Capital | 4 | 2 | 2 | 4+ | Anti-strike with free strike attack; participates in Contact and Battle |
| **Cruiser** | Capital | 5 | 2 | 3 | 4+ | Heavy anti-capital (bonus vs Capital) with planetary bombard; needs escort against strike craft; capacity 2 (any mix of strike craft and planetary units) |
| **Carrier** | Capital | 6 | 2 | 4 | 5+ | Transport; capacity 8 (any mix of strike craft and planetary units); has **shield** |

### Strike

| Unit | Category | Cost | Hull | Silhouette | Base hit | Combat role |
|---|---|---|---|---|---|---|
| **Interceptor** | Strike | 1 | 1 | 1 | 4+ | Counters strike craft in Contact phase; absent from Battle; cannot attack ships |
| **Fighter** | Strike | 1 | 1 | 1 | 4+ | General-purpose; engages strike craft and capital ships (penalty vs Capital) |
| **Bomber** | Strike | 2 | 1 | 1 | 4+ | Attacks capital ships and planetary units; penalty vs strike craft; ignores shields |

### Planetary

| Unit | Category | Cost | Hull | Silhouette | Base hit | Combat role |
|---|---|---|---|---|---|---|
| **Infantry** | Planetary | 1 | 1 | 1 | 4+ | General ground combat |
| **Armor** | Planetary | 2 | 1 | 2 | 4+ | Armored ground assault; has **shield** (absorbs first hit per turn) |

Only planetary units may begin **Nexus Gate** construction.

**Starting composition:** 1 Carrier, 4 Infantry, 2 Fighters — placed at the player's home system at game start.

---

## Round Structure

Each round follows this sequence:

1. **Plan phase** — both players simultaneously and secretly commit orders
2. **Resolve phase:**
   1. Build cost deducted · Nexus Gate payment deducted
  2. Moves — all units move simultaneously. If units arrive at a system occupied by enemy units, they stop and combat resolves in step 3. Planetary units arriving at an opponent-controlled or uncontrolled system with no enemy units present can establish control immediately.
  3. Combat — all contested systems resolve in spiral order (Nexus → Ring 1 → Ring 2, homes last); each system runs Contact then Battle before the next system resolves
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

A player **controls** a system **only** when they have at least one planetary unit in it and the opponent has no units there. Control is the mechanism for earning income from a system.

**Establishing control:** Planetary units arriving at an uncontrolled system or an opponent-controlled system with no enemy units present take control of it immediately during the Moves step. No order is required — presence is sufficient.

**Losing control from planetary departure:** Control is lost immediately when a player no longer has any planetary units on the system, even if friendly capital ships or strike craft remain. If a player moves their last planetary unit away, the system becomes uncontrolled (not retained by the departing player's remaining ships).

**Contested system:** A system with units from both players present is contested — neither player controls it and it produces no income for either player. All units present participate in combat (see [Combat](#combat)). Planetary units in a contested system are locked and cannot move while the system remains contested. Once the system is no longer contested, surviving planetary units are free to move again. If all planetary units on both sides are eliminated in combat, the system becomes uncontrolled.

**Capital ships and strike craft** cannot establish or retain control on their own. They can, however, contest an existing controller while they remain in the system. A system with only capital ships and/or strike craft (no planetary units) is always uncontrolled.

**Uncontrolled system:** A system that has never been captured, or where combat eliminated all planetary units, or where the controlling player's last planetary unit moved away. Produces no income until one player's planetary units arrive.

**Home systems** are under each player's control from game start (starting with 4 Infantry) and follow the same rules as any other system — they can be captured if the opponent's planetary units arrive uncontested.

**The Nexus** cannot be controlled for income regardless of planetary unit presence.

---

## Combat

When units from both players occupy the same system after moves resolve, combat resolves in two sequential **exchange phases**: **Contact** then **Battle**. If multiple systems are contested, they resolve in **spiral order** (Nexus first, then Ring 1 clockwise from NE, then Ring 2 clockwise from NE, with home systems last). Each system's combat completes fully before the next contested system begins. Each phase is an attrition exchange — both sides roll simultaneously, casualties are applied after all dice resolve, and survivors carry forward. Neither side retreats; both may remain on a contested system after all phases complete.

### Dice System

Each unit rolls 1d6 per attack in each phase it participates in. A result at or above the unit's **effective hit threshold** scores 1 hit on an enemy unit. Each hit randomly targets one enemy unit; the probability of a unit being selected is proportional to its **silhouette** (targeting weight). A unit is destroyed when it has absorbed hits equal to its **hull** (HP). Units fight at full strength until destroyed. The minimum hit threshold is 2 (a threshold below 2 always hits).

### Shields

Some units have a **shield** that may absorb incoming hits. When a shielded unit would take a hit, roll 1d6: on a **4+** the shield absorbs the hit — the hit is negated and the shield is consumed for the rest of that turn. On a **1–3** the hit passes through to hull and the shield remains active (it may still attempt to absorb a later hit this turn). The shield regenerates at the end of each turn's combat (after Battle resolves). A shield-absorbed hit is recorded in the combat log as "absorbed" rather than "hit". The Frigate, Carrier, and Armor are currently the only shielded units.

### Escort

The **Frigate** has the Escort tag. Each Frigate in a system protects one non-Escort Capital ship by reducing its effective silhouette by 1 (minimum 1). The ships with the highest silhouette are covered first. Escort does not stack.

### Free extra attacks

The **Destroyer** has one free extra attack per phase that only targets Strike units, in addition to its base attacks.

### Targeting System

Each unit type has a **base hit threshold** and a set of **tags** that determine:
- Which phases it participates in (Contact, Battle, or both)
- Which enemy categories it can target per phase
- Bonus or penalty modifiers to its hit threshold vs specific categories

The effective hit threshold against a given target category is:
```
base threshold
  − 1 if the attacker has BonusVs{Category}
  + 1 if the attacker has PenaltyVs{Category}
  minimum 2
```

#### Phase eligibility (targeting tags)

| Phase | Targeting tag |
|---|---|
| **Contact** | `FirstAttack{Category}` — attack resolves before return fire from units that only target in Battle |
| **Battle** | `CanAttack{Category}` — standard engagement |

A unit can only be targeted by attacks in phases where it is present and targetable (see Participation table).

### Participation & Hit Thresholds

**A** = attacks · **T** = targetable · **—** = not present this phase

| Unit | Contact | Battle | Base hit | vs Strike | vs Capital | vs Planetary | Special |
|---|---|---|---|---|---|---|---|
| **Interceptor** | A · T | — | 4 | 4 (FirstAttack) | — | — | Strike-only in Contact |
| **Fighter** | — | A · T | 4 | 4 | 5 (penalty) | — | — |
| **Bomber** | — | A · T | 4 | 5 (penalty) | 4 | 4 | Ignores shields |
| **Destroyer** | A · T | A · T | 4 | 4 | 4 | — | FreeAttack vs Strike |
| **Frigate** | — | A · T | 4 | 4 | 4 | — | Shield, Escort |
| **Cruiser** | — | A · T | 4 | 4 | 3 (bonus) | 4 | Capacity 2 |
| **Carrier** | — | A · T | 5 | 5 | 5 | — | Shield, Capacity 8 |
| **Infantry** | — | A · T | 4 | — | — | 4 | — |
| **Armor** | — | A · T | 4 | — | — | 4 | Shield |

Silhouette-weighted random targeting applies only within the eligible target pool for the attacking unit's current phase. A unit cannot be targeted in a phase where it is marked **—**.

### Phase 1 — Contact

Strike craft and Destroyers that have `FirstAttack{Category}` tags attack eligible targets. Currently only **Interceptors** participate (FirstAttackStrike), attacking enemy strike craft. **Destroyers** also participate in Contact (targeting both Strike and Capital). Eligible attackers cannot be targeted by units that only participate in Battle. Skipped if neither side has any eligible attackers.

### Phase 2 — Battle

All remaining units participate according to their `CanAttack{Category}` tags. All surviving units from Contact are present and targetable. This is the main engagement phase. Skipped if neither side has any eligible units.

### System Outcome

After both combat phases, control is determined by planetary unit presence (see [System Control](#system-control)).

- **Both sides have surviving units:** system remains contested — neither controls it, no income for either
- **One side eliminated:** surviving player may control if they have planetary units present
- **No planetary units from either side:** system becomes uncontrolled



---

## Win Condition

**Build a Nexus Gate** at the center system:

- At least one of your planetary units must occupy an uncontested Nexus; declare **Begin Nexus Gate**
- Total cost: **24 Energy**, committed over **2 consecutive turns** (12 Energy per turn)
- **Turn N:** declare construction, commit 12 Energy; construction status is visible to both players
- **Turn N+1:** commit 12 Energy; gate completes — you win
- Construction is cancelled if: all planetary units on the Nexus are eliminated in combat; the planetary units move away voluntarily; or the player does not declare `BeginNexusGate` on the following turn. In all cases, committed Energy is forfeited — no refund.
- The construction check happens after combat resolves — at least one planetary unit must survive combat on the Nexus and the Nexus must still be uncontested for construction to proceed that turn
- If both players complete the Nexus Gate in the same turn: **draw**

---

## Resolve Events

The resolve phase emits a typed sequence of events. The front end consumes these to update board state and render the round log. Events are produced in resolution order; skipped steps (e.g. a combat phase with no eligible units) produce no events. The message rendering in [Resolve Phase Messages](#resolve-phase-messages) derives from this stream.

### Movement

| Event | Fired when | Key data |
|---|---|---|
| `NexusUnitsMovedEvent` | A player's units leave one system and arrive at another | `PlayerId`, `From`, `To`, `Stacks` (unit type, hits, count), `IsRetreat` |

`IsRetreat = true` when the source system was contested before moves resolved (the player is moving out of a fight). Retreat moves and normal moves use the same event type.

### System Control

| Event | Fired when | Key data |
|---|---|---|
| `NexusPlanetaryControlEvent` | A player gains or retains sole planetary presence in a system | `System`, `PlayerId` |
| `NexusSystemContestedEvent` | Both players have units in the same system — no income for either | `System` |
| `NexusSystemUncontrolledEvent` | All planetary units are gone from a system — control cleared | `System` |

### Combat

| Event | Fired when | Key data |
|---|---|---|
| `NexusCombatBeganEvent` | Combat is about to resolve at a system | `System`, `Player1Id`, `Player2Id` |
| `NexusCombatStepEvent` | One combat phase (Contact/Battle) completes | `System`, `Phase` (Contact/Battle), `Losses` (per player/type/count), `AttackRolls` (individual dice) |
| `NexusSystemClearedEvent` | All units of one player are eliminated from a system | `System`, `VictorId` |

Only phases where at least one side has eligible units produce a `NexusCombatStepEvent`. `AttackRolls` carries every individual die roll (attacker type, target type, roll, threshold, hit/miss) to support detailed log rendering.

### Income & Deployment

| Event | Fired when | Key data |
|---|---|---|
| `NexusIncomeEvent` | A player collects income for the round | `PlayerId`, `Amount`, `Sources` (list of contributing systems) |
| `NexusUnitDeployedEvent` | A newly built unit appears at the player's home system | `PlayerId`, `UnitType`, `HomeSystem`, `Count` |

One `NexusUnitDeployedEvent` fires per unit type per build order; multiple orders of the same type in one turn produce separate events.

### Supply

| Event | Fired when | Key data |
|---|---|---|
| `NexusCapitalDisbandedEvent` | A Capital is removed because it exceeds the player's supply pool | `PlayerId`, `UnitType`, `System`, `Count` |

Events fire in spiral order (Nexus → Ring 1 → Ring 2), one per unit type per system. All disband events for a given round resolve before the Gate check.

### Nexus Gate

| Event | Fired when | Key data |
|---|---|---|
| `NexusGateStartedEvent` | A player commits the first 12 Energy; construction begun | `PlayerId`, `System` |
| `NexusGateCompletedEvent` | A player commits the second 12 Energy; gate complete | `PlayerId`, `System` |
| `NexusGateCancelledEvent` | Construction cancelled — planetary units lost, voluntarily moved, or gate not re-declared | `PlayerId`, `System` |

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

### Combat (one block per contested system, two phases)

| Event | Message |
|---|---|
| Combat begins | `Combat at [System] — [A]: [summary] vs [B]: [summary]` |
| Phase 1 result | `Contact at [System] — [A] loses [N] unit(s), [B] loses [N] unit(s)` |
| Phase 2 result | `Battle at [System] — [A] loses [N] unit(s), [B] loses [N] unit(s)` |
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
| Gate construction cancelled | `[A]'s Nexus Gate construction was cancelled` |
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



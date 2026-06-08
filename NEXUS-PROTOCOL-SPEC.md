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

- **Nexus** `[N]` — center system; win site only; Energy=0, Supply=0; cannot be controlled for income
- **Home systems** — two opposing outer systems: `(2,-2)` for Player 1, `(-2,2)` for Player 2; each produces **2 Energy/turn** and **2 Supply**; under that player's control from game start
- **16 income systems** — every remaining system; each has a fixed (Energy, Supply) stat pair drawn from the 8 combat profiles below
- Every system's Energy and Supply values are visible to both players from turn 1
- Map is fully visible from turn 1

### System Stat Profiles

The 16 non-home, non-Nexus systems use 8 distinct stat profiles, each appearing exactly twice on the map:

| Profile | Name | Energy | Supply | Strategic character |
|---|---|---|---|---|
| A | Outpost | 0 | 1 | Fleet anchor — no income, but supports +1 Capital |
| B | Depot | 0 | 2 | High fleet capacity, zero income |
| C | Refinery | 1 | 0 | Funds builds, no fleet support |
| D | Colony | 1 | 1 | Solid all-rounder |
| E | Garrison | 1 | 2 | Modest income, excellent fleet support |
| F | Trade Port | 2 | 0 | Rich income, no fleet support |
| G | Core World | 2 | 1 | Rich + decent supply — prime real estate |
| H | Capital | 2 | 2 | Everyone fights over these |

### Map Layout

Ring 1 (clockwise from NE, closer to Nexus):
- Alpha — **Core World** (2,1)
- Beta — **Colony** (1,1)
- Gamma — **Capital** (2,2)
- Delta — **Trade Port** (2,0)
- Epsilon — **Refinery** (1,0)
- Zeta — **Outpost** (0,1)

Ring 2 (clockwise from NE):
- Eta — **Colony** (1,1)
- Theta — **Depot** (0,2)
- Iota — **Refinery** (1,0)
- Kappa — **Garrison** (1,2)
- Lambda — **Capital** (2,2)
- Mu — **Outpost** (0,1)
- Nu — **Core World** (2,1)
- Xi — **Depot** (0,2)
- Omicron — **Trade Port** (2,0)
- Pi — **Garrison** (1,2)

### Sector Names

Ring 1 systems (clockwise from NE): Alpha, Beta, Gamma, Delta, Epsilon, Zeta  
Ring 2 systems (clockwise from NE): Eta, Theta, Iota, Kappa, Lambda, Mu, Nu, Xi, Omicron, Pi

---

## Resources

Two resource types: **Energy** and **Supply**.

### Energy (spending resource)

| Source | Income |
|---|---|
| Home system | +2 Energy/turn |
| Income system | 0–2 Energy/turn (per-system stat) |

- Controlling a system earns its Energy income; the opponent loses that income if they previously controlled it
- Energy has no upper limit; unused Energy carries over between turns
- Each player begins with **5 Energy**
- Energy is spent on: building units, paying Nexus Gate instalments

### Supply (fleet capacity)

| Source | Supply |
|---|---|
| Home system | +2 Supply |
| Income system | 0–2 Supply (per-system stat) |

- A player's **supply pool** = total Supply value of all systems they currently control
- Supply determines the maximum number of Capital-hull ships they can sustain
- Only Capital hull ships draw against supply. Strike craft and planetary units are always free to hold
- Uncontrolled and contested systems contribute 0 supply, regardless of units present there
- Supply is a pool, not income — it doesn't accumulate or carry over. It sets a cap checked each round

---

## Supply Check — automatic disbanding

After newly built units deploy each round, if a player's total Capital count exceeds their supply pool, the excess Capitals are automatically disbanded. Disbanding evaluates systems in **spiral order** — Nexus first, then Ring 1 clockwise from NE (Alpha → Zeta), then Ring 2 clockwise from NE (Eta → Pi → home systems). Within each system, Capitals are disbanded in order of cheapest **design cost** first; ties are broken by lowest remaining hits (weakest unit first). Disbanding continues until Capitals ≤ supply.

**Strategic consequences:**
- Capitals pushed forward toward the Nexus are the first lost when supply runs short
- Losing controlled systems in combat shrinks the supply pool immediately — a good attack can collapse the opponent's fleet *and* their economy in the same round
- Committing energy to the Nexus Gate (rather than new Capitals) leaves the existing fleet vulnerable if income drops
- Home systems are the last place disbanding reaches; home-fleet Capitals are the safest to hold

---

## Unit Designs

There are **no fixed unit types**. Instead, each player designs their own units by selecting a **hull category** and attaching **modules**. All designs are visible to both players.

### Hull Categories

| Hull | Base Cost | Base Hits | Base Silhouette | Base Move | Slot Budget | Description |
|---|---|---|---|---|---|---|
| **Strike** | 1 | 1 | 1 | 0 | 2 | Strike craft — immobile without Drive; must be carried via Hangar |
| **Capital** | 2 | 2 | 2 | 1 | 4 | Ships — move independently; provide carry capacity via Hangar module |
| **Planetary** | 1 | 1 | 1 | 0 | 2 | Ground units — immobile without Drive; must be carried via Hangar |

**Slot budget** limits the total module "slot cost" a design may carry (see Module Costs below).

### Modules

| Module | Cost | Slots | Allowed Hulls | Effect |
|---|---|---|---|---|
| Shield | 2 | 1 | All | Absorbs first hit each combat turn on 4+ save |
| Disruptor | 2 | 1 | All | Attacks bypass Shield saves entirely |
| Screen(N) | N | N | All | Reduces effective silhouette of N friendly Capitals by 1 (min 1) when attacked by chosen category |
| Command(N) | N×2 | N | All | Reduces hit threshold of N friendly same-category units by 1 (highest silhouette buffed first) |
| Dock | 0 | 0 | Strike, Planetary | This unit can be transported (consumes 1 carry slot) |
| Hangar(C) | C | (C+1)/2 | Capital | Provides C capacity for Dock units |
| Battery(C) | 1 | 1 | All | Base attacks against category C resolve in Battle phase |
| Vanguard(C) | 2 | 1 | All | Base attacks against category C resolve in Contact phase (safe from return fire) |
| Seeker(M) | M×2 | M | All | Hit threshold reduced by M vs chosen category (min 2) |
| Scatter(M) | −M | 0 | All | Hit threshold increased by M vs chosen category (penalty — costs negative) |
| Cloak(N) | N×2 | N | All | Reduces silhouette by N (min 1), harder to target |
| Armour(N) | N×2 | N | All | Increases hit points by N |
| Control | 1 | 0 | Planetary | Enables system control determination (planetary units only) |
| Drive(N) | N×2 | N | Strike, Capital | Increases move range by N hexes |
| Repair | 3 | 1 | All | Restores 1 lost hit per turn after combat |
| Bulkhead(N) | N×2 | −N | All | Grants N extra module slots; increases silhouette by N |
| Beacon(N) | 0 | 1 | All | Increases silhouette by N (max 1), making unit more likely targeted |

**Constraints:**
- Shield and Disruptor are mutually exclusive (only one of each per design)
- Beacon and Cloak are mutually exclusive
- Each hull category may only have one Battery(C) per target category
- Each hull category may only have one Vanguard(C) per target category
- Seeker and Scatter are mutually exclusive for the same target category
- Duplicate Dock, Control, Repair, Bulkhead are not allowed

### Attack Derivation

Each module that grants attacks specifies a target category and a phase:

- **Battery(C)** → base attacks in **Battle** phase against category C
- **Vanguard(C)** → base attacks in **Contact** phase against category C

Effective hit threshold per attack:

```
base 4
  − SeekerMagnitude[category]
  + ScatterMagnitude[category]
  − 1 if friendly Command(C) covers this unit
  minimum 2
```

### Default Designs

Every player starts with three free designs (not yet built — just the blueprints):

| Name | Hull | Modules | Cost | Hits | Move | Silhouette | Carry |
|---|---|---|---|---|---|---|---|
| Fighter | Strike | Battery(Strike), Battery(Capital), Dock | 3 | 1 | 0 | 1 | 0 |
| Light Freighter | Capital | Hangar(2) | 4 | 2 | 1 | 2 | 2 |
| Light Tank | Planetary | Battery(Planetary), Control, Dock | 3 | 1 | 0 | 1 | 0 |

### Design Management

Players can create and delete designs between rounds (when not in combat resolution) via:

- **CreateDesign** — pick a hull, name the design, select modules (subject to slot budget and constraints)
- **DeleteDesign** — removes a design; cannot delete while units of that design exist on the map

---

## Orders

Each unit may be given one order per turn. A unit with no order stays in place.

| Order | Effect |
|---|---|
| **Move** | Select any movable units on this system and a path of adjacent destination systems (waypoints); move them together through the path. Capital ships in the selection provide carry capacity via Hangar modules. Each Dock unit consumes 1 capacity slot. The path cannot revisit a system and cannot pass through a system occupied by enemy Strike or Capital units (planetary-only systems can be traversed). The fleet's maximum move range is the minimum `Drive` value across all Capital ships in the selection. Planetary units in a contested system are locked and cannot be selected. |
| **Build [design]** | At home system only; costs vary by design; any number of units may be built per turn; units appear at end of Resolve and can receive orders the following turn |
| **Begin Nexus Gate** | At least one planetary unit with the Control module must be present on an uncontested Nexus; commits resources toward construction |

Multiple orders may originate from the same system in the same turn (e.g. one set of units moves, another stays).

### Movement Detail

- A Move order selects any subset of units on a system and specifies a path (list of adjacent waypoints); all selected units move together through each waypoint
- Ships provide carry capacity equal to the sum of all Hangar modules on Capital ships in the selection
- Dock units (strike craft and planetary) may only move if included alongside Capital ships whose combined Hangar capacity covers them; each Dock unit consumes 1 capacity slot
- The maximum path length is the minimum `Move` value across all Capital ships in the fleet (strike craft and planetary with Drive > 0 can move independently, reducing the minimum interceptor)
- Waypoints cannot contain enemy fleets (Strike or Capital units present); the final destination can
- Planetary units in a contested system are locked in place until that system is no longer contested
- A system may contain any number of units from either player; units not included in a move order stay in place

---

## System Control

A player **controls** a system **only** when they have at least one planetary unit with the **Control** module in it and the opponent has no units there. Control is the mechanism for earning Energy income and contributing Supply from a system.

**Establishing control:** Planetary units (with Control) arriving at an uncontrolled system or an opponent-controlled system with no enemy units present take control of it immediately during the Moves step. No order is required — presence is sufficient.

**Losing control from planetary departure:** Control is lost immediately when a player no longer has any Control-bearing planetary units on the system, even if friendly capital ships or strike craft remain. If a player moves their last planetary unit away, the system becomes uncontrolled (not retained by the departing player's remaining ships).

**Home systems:** If a home system loses all planetary units and neither player has Control units there, control reverts to the home system's owner automatically.

**Contested system:** A system with units from both players present is contested — neither player controls it. It produces no Energy income and contributes 0 Supply for either player. All units present participate in combat. Planetary units in a contested system are locked and cannot move while the system remains contested. Once the system is no longer contested, surviving planetary units are free to move again. If all planetary units on both sides are eliminated in combat, the system becomes uncontrolled.

**Capital ships and strike craft** cannot establish or retain control on their own. They can, however, contest an existing controller while they remain in the system. A system with only capital ships and/or strike craft (no Control planetary units) is always uncontrolled.

**Uncontrolled system:** A system that has never been captured, or where combat eliminated all planetary units, or where the controlling player's last planetary unit moved away. Produces no Energy income and contributes 0 Supply until one player's planetary units arrive.

**Home systems** are under each player's control from game start and follow the same rules as any other system — they can be captured if the opponent's planetary units arrive uncontested.

**The Nexus** cannot be controlled for Energy income or Supply contribution regardless of planetary unit presence.

---

## Combat

When units from both players occupy the same system after moves resolve, combat resolves in two sequential **exchange phases**: **Contact** then **Battle**. If multiple systems are contested, they resolve in **spiral order** (Nexus first, then Ring 1 clockwise from NE, then Ring 2 clockwise from NE, with home systems last). Each system's combat completes fully before the next contested system begins. Each phase is an attrition exchange — both sides roll simultaneously, casualties are applied after all dice resolve, and survivors carry forward. Neither side retreats; both may remain on a contested system after all phases complete.

### Dice System

Each unit rolls 1d6 per attack in each phase it participates in. A result at or above the unit's **effective hit threshold** scores 1 hit on an enemy unit. Each hit randomly targets one enemy unit; the probability of a unit being selected is proportional to its **silhouette** (targeting weight). A unit is destroyed when it has absorbed hits equal to its **hits** (HP). Units fight at full strength until destroyed. The minimum hit threshold is 2 (a threshold below 2 always hits).

### Phase 1 — Contact

Any unit with **Vanguard** modules resolves its attacks in this phase. Currently only Strike-category units with Vanguard(Category) modules participate. Eligible attackers cannot be targeted by units that only participate in Battle. Skipped if neither side has any eligible attackers.

### Phase 2 — Battle

Any unit with **Battery** modules resolves its attacks in this phase. All surviving units from Contact are present and targetable. This is the main engagement phase. Skipped if neither side has any eligible units.

### Shields

Units with the **Shield** module may absorb incoming hits. When a shielded unit would take a hit (and the attacker does not have the **Disruptor** module), roll 1d6: on a **4+** the shield absorbs the hit — the hit is negated and the shield is consumed for the rest of that turn. On a **1–3** the hit passes through to hull and the shield remains active (it may still attempt to absorb a later hit this turn). The shield regenerates at the end of each turn's combat (after Battle resolves). A shield-absorbed hit is recorded in the combat log as "absorbed" rather than "hit".

### Screen (Escort)

The **Screen** module reduces the effective silhouette of friendly Capital ships without Screen by 1 (minimum 1) when attacked by units of the specified category. The ships with the highest silhouette are covered first. Screen does not stack on the same ship.

### Command

The **Command** module reduces the effective hit threshold by 1 for up to N friendly same-category units that are not themselves Command providers. Highest silhouette units are buffed first.

### Repair

After combat resolves each round, units with the **Repair** module restore 1 lost hit (up to their maximum).

### Targeting

Each unit's effective hit threshold against a given target category is:

```
base 4
  − SeekerMagnitude(attacker, targetCategory)
  + ScatterMagnitude(attacker, targetCategory)
  − 1 if Command covers this unit
  minimum 2
```

A unit can only target categories for which it has a **Battery** (Battle) or **Vanguard** (Contact) module. Silhouette-weighted random targeting applies only within the eligible target pool for the attacking unit's current phase.

### System Outcome

After both combat phases, control is determined by planetary unit presence with the Control module (see [System Control](#system-control)).

- **Both sides have surviving units:** system remains contested — neither controls it, no income for either
- **One side eliminated:** surviving player may control if they have planetary units with Control present
- **No planetary units with Control from either side:** system becomes uncontrolled (or reverts to home system owner)

---

## Win Condition

**Build a Nexus Gate** at the center system:

- At least one of your planetary units (with Control module) must occupy an uncontested Nexus; declare **Begin Nexus Gate**
- Total cost: **24 Energy**, committed over **2 consecutive turns** (12 Energy per turn)
- **Turn N:** declare construction, commit 12 Energy; construction status is visible to both players
- **Turn N+1:** commit 12 Energy; gate completes — you win
- Construction is cancelled if: all planetary units on the Nexus are eliminated in combat; the planetary units move away voluntarily; or the player does not declare `BeginNexusGate` on the following turn. In all cases, committed Energy is forfeited — no refund.
- The construction check happens after combat resolves — at least one planetary unit must survive combat on the Nexus and the Nexus must still be uncontested for construction to proceed that turn
- If both players complete the Nexus Gate in the same turn: **draw**

---

## Round Structure

Each round follows this sequence:

1. **Plan phase** — both players simultaneously and secretly commit orders
2. **Resolve phase:**
   1. Build cost deducted · Nexus Gate payment deducted
   2. Moves — all units move simultaneously through their waypoint paths. If units arrive at a system occupied by enemy units, they stop and combat resolves in step 3. If waypoints contain enemy fleets (Strike or Capital), the move is rejected at validation time. Planetary units arriving at an opponent-controlled or uncontrolled system with no enemy units present can establish control immediately.
   3. Combat — all contested systems resolve in spiral order (Nexus → Ring 1 → Ring 2, homes last); each system runs Contact then Battle before the next system resolves
   4. Repair — units with the Repair module restore 1 lost hit
   5. Income — all Energy income calculated and applied simultaneously; a player collects Energy from every system they control
   6. Newly built units appear at home system
   7. Supply check — if a player's Capital count exceeds their supply pool, excess Capitals are automatically disbanded in spiral order (see Supply section)
   8. Gate progress and win check
3. **Win check**

---

## Resolve Events

The resolve phase emits a typed sequence of events. The front end consumes these to update board state and render the round log. Events are produced in resolution order; skipped steps produce no events.

### Movement

| Event | Fired when | Key data |
|---|---|---|
| `NexusUnitsMovedEvent` | A player's units leave one system and arrive at another | `PlayerId`, `From`, `To`, `Stacks` (design ID, category, hits, count), `IsRetreat` |

`IsRetreat = true` when the source system was contested before moves resolved (the player is moving out of a fight). Retreat moves and normal moves use the same event type.

### System Control

| Event | Fired when | Key data |
|---|---|---|
| `NexusPlanetaryControlEvent` | A player gains or retains sole planetary presence in a system | `System`, `PlayerId` |
| `NexusSystemContestedEvent` | Both players have units in the same system — no income for either | `System` |
| `NexusSystemUncontrolledEvent` | All planetary units (with Control module) are gone — control cleared | `System` |

### Combat

| Event | Fired when | Key data |
|---|---|---|
| `NexusCombatResultEvent` | All combat phases at one contested system complete | `System`, `Player1Id`, `Player2Id`, `Phases` (array of `NexusPhaseResult`) |
| `NexusSystemClearedEvent` | All units of one player are eliminated from a system | `System`, `VictorId` |

Each `NexusPhaseResult` contains:
- `Phase` — Contact or Battle
- `Losses` — per-player, per-design, count
- `AttackRolls` — every individual die roll (attacker design, target design, roll, threshold, hit/miss, shield status)

Only phases where at least one side has eligible units produce a `NexusPhaseResult`. If no phase has eligible units, no `NexusCombatResultEvent` fires.

### Income & Deployment

| Event | Fired when | Key data |
|---|---|---|
| `NexusIncomeEvent` | A player collects Energy income for the round | `PlayerId`, `Amount`, `Sources` (list of contributing system coords) |
| `NexusUnitDeployedEvent` | A newly built unit appears at the player's home system | `PlayerId`, `DesignId`, `DesignName`, `HomeSystem`, `Count` |

One `NexusUnitDeployedEvent` fires per design per build order; multiple orders of the same design in one turn produce separate events.

### Supply

| Event | Fired when | Key data |
|---|---|---|
| `NexusCapitalDisbandedEvent` | A Capital is removed because it exceeds the player's Supply pool | `PlayerId`, `DesignId`, `DesignName`, `System`, `Count` |

Events fire in spiral order (Nexus → Ring 1 → Ring 2), one per design per system. All disband events for a given round resolve before the Gate check.

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
| Units move | `[A]'s units advanced from [System] to [System]: [count]× [design]` |
| Units retreat | `[A]'s units retreated from [System] to [System]: [count]× [design]` |
| Planetary units take control | `[A] took control of [System]` |

### Combat (one block per contested system, two phases)

| Event | Message |
|---|---|
| Combat begins | `Combat at [System] — [A] vs [B]` |
| Phase result | `[Phase]: [A] [design] (X hits) -> [B] [design] (Y hits): rolled Z vs W hit/miss/absorbed` |
| Phase summary | `Losses: [A] loses N× [design]; [B] loses N× [design]` |
| System cleared (one side eliminated) | `[A] cleared [System]` |

### Income

| Event | Message |
|---|---|
| Income received | `[A] collected +[N]⚡ Energy from [M] system(s)` |

### Deployment

| Event | Message |
|---|---|
| Unit deployed | `[A] deployed [N]× [design] at [Home]` |

### Supply Check

| Event | Message |
|---|---|
| Capital disbanded | `[A]'s [design] at [System] was disbanded (over supply limit)` |

### Win Check

| Event | Message |
|---|---|
| Gate construction begun (turn 1 of 2) | `[A] began Nexus Gate construction at [System]` |
| Gate construction completes (turn 2 of 2) | `[A] completed the Nexus Gate at [System]!` |
| Gate construction cancelled | `[A]'s Nexus Gate construction at [System] was cancelled` |
| Victory by gate | `[A] activated the Nexus Gate — victory!` |
| Draw by simultaneous gate | `The game ended in a draw: [reason]` |

---

## Game Start

Games are created via invite link:

1. Player A creates a game — receives a unique invite URL
2. Player A shares the URL with Player B
3. Player B opens the URL and joins — the game begins immediately
4. There is no turn timer; the game is fully async (players plan and submit whenever ready)
5. Each player's submitted orders are revealed and resolved only once **both** players have submitted for that round
6. Each player starts with **5 Energy** and the three default designs (Fighter, Light Freighter, Light Tank)

---

## UX — Game Screen

### Wireframe — Planning Phase

```
┌──────────────────────────────────────────────────────────────────────────────────────┐
│  Nexus Protocol · Round 4 · PLANNING                          [⚙ 1/2] [Submit]    │
├────────────────┬─────────────────────────────────────┬───────────────────────────────┤
│                │                                     │  Round 4 — Planning           │
│  YOU  (Blue)   │         ⚡1⬡1 ⚡2⬡1                   │  ──────────────────────       │
│  ⚡ 12 → 8    │       ⚡2⬡2 ·  ·  · ⚡1⬡0              │  ○ Combat at Gamma            │
│  ⬡ 6 (4 cap) │     ·  ·  · [N] ·  ·  ·              │    A loses 1 Light Tank       │
│                │   ⚡0⬡1 ·  ·  ·  · ⚡2⬡0               │  ○ A took control of Theta    │
│  OPPONENT (Red)│       ⚡1⬡1 ·  ·  ⚡0⬡2               │  ○ A collected +7⚡ Energy    │
│  ⚡ 24         │         ⚡2⬡1 ⚡1⬡1                   │                               │
│  ⬡ 8 (3 cap) │                                     │  Round 3                      │
│                │                                     │  ──────────────────────       │
│  ─────────────│                                     │  ○ A advanced to Alpha        │
│                │   Legend:                           │    × Light Freighter          │
│  PENDING       │   ⚡ = Energy  ⬡ = Supply            │  ○ B's Capital at Theta       │
│                │   ◆ = your units  ◇ = enemy         │    disbanded (over supply)    │
│  Alpha → Beta │                                     │                               │
│    LF + LT[×] │                                     │                               │
│                │                                     │  ──────────────────────       │
│  Build Fighter │                                     │  You:  nice push              │
│    [×]         │                                     │  Them:  wait til next turn    │
│                │                                     │                               │
│                │                                     │ ┌─────────────────────────┐   │
│                │                                     │ │ Type a message…       ↵│   │
│                │                                     │ └─────────────────────────┘   │
└────────────────┴─────────────────────────────────────┴───────────────────────────────┘
```

**System display (zoomed — your controlled system, mid-game):**
```
    ╔══════════╗
    ║  Vega    ║   ← sector name (non-home, non-Nexus systems only)
    ║  ⚡2 ⬡1  │   ← Energy (⚡) + Supply (⬡); monospace
    ║    ◆     ║   ← your presence diamond (faction color)
    ╚══════════╝
       (blue tint = you control)
```

When both factions have units on the same system, two diamonds appear side by side — yours offset left, opponent's offset right:
```
    ╔══════════╗
    ║  ⚡2 ⬡1  │
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
- **Left sidebar:** both players' resource totals — yours on top, opponent below; fully visible at all times (open information). Each player shows Energy and Supply pool (with Capital count):

  ```
  YOU  (Blue)
  ⚡ 12 → 8
  ⬡ 6  (Capitals: 4)
  ```

During Planning phase your Energy shows the projected balance after all queued build costs (e.g. "12 → 8"). Pending orders list below resources during Planning phase.
- **Center:** hex grid — primary interactive area; all orders assigned via grid interaction
- **Right sidebar:** game log and player chat interleaved chronologically; message input pinned at the bottom

### System Display

Each system shows:
- **Background tint / border:** control indicator (your color / opponent color / contested / uncontrolled gray); Nexus uses a distinct neutral style
- **Resource labels:** Energy value (⚡) and Supply value (⬡) shown on every income system; home systems show `⚡2 ⬡2`; Nexus shows no resources
- **Unit presence:** a small colored diamond renders in the lower portion of the hex for each faction that has units there; your faction's diamond is offset left when both are present, opponent's right; no numeric count is shown on the hex
- **Nexus Gate badge:** when your gate construction is in progress, two small pip circles render on the Nexus hex; the first pip is lit in your faction color when stage 1 is complete, the second when stage 2 is complete

### Order Assignment — Planning Phase

Orders are assigned system-first: click a system to open a unit selection panel, configure the move, then click the destination.

**Assigning a Move order:**
1. **Click a system** — if it contains your units, a unit selection panel appears anchored to that system; each design is shown with its count and a toggle
2. **Toggle units** in the panel to include them in the selection
3. **Destination systems highlight** on the grid based on the current selection and the fleet's movement range; systems are grayed if capacity is insufficient for the selected strike craft and planetary units
4. **Click a highlighted destination system** — the Move order is created; the panel closes; an arrow overlay appears from source to destination
5. **Click elsewhere or close** — closes the panel without assigning an order

To create a second Move order from the same system (e.g. send some units to system A, others to system B): after the first order is assigned, click the system again — the panel reopens showing only designs that have not yet been assigned an order.

**Assigning Begin Nexus Gate:** click the Nexus system while your planetary units are on it — the unit panel shows a "Begin Nexus Gate" action alongside the unit toggles; activate it to queue the order.

**Build [design]:** click the home system background (not a unit in the panel) — a build panel appears listing available designs with their costs, greyed out if the projected Energy balance is insufficient; click a design to queue it. Multiple builds may be queued in one turn.

### Pending Orders List

During Planning phase, the bottom of the left sidebar shows all orders queued so far this turn:

- **One row per Move order**, showing the fleet composition and destination (e.g. `Light Freighter + 2 Light Tank → Vega`); × removes the entire move and returns all units in it to unassigned
- **One row per Build order** (e.g. `Build Fighter`); × cancels it and restores the projected Energy
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
| Moves | Unit icons slide to destination systems simultaneously through waypoints |
| Combat — each phase | Contested system flashes per phase; unit count numbers animate down |
| Control change | System control color transitions |
| Income | Resource counters tick up |
| Unit appears | New unit icon fades in at home system |

**Timing:** Fixed delay — each log entry appears and its grid animation plays before the next event fires.

**Multiple simultaneous combats** are displayed sequentially in ring-inward order: ring-2 hexes first, ring-1 next, Nexus last. This gives a natural narrative feel of conflict sweeping toward the center.

---

## Design Editor

Players can manage their unit designs from the game screen before submitting orders:

- Click a **Design Editor** tab or button to open the design panel
- View all owned designs with their full stats (cost, hits, silhouette, attacks, modules, move, carry capacity)
- **Create Design:** select a hull category → name the design → add modules from the available list (subject to slot budget and constraints)
- **Delete Design:** confirm deletion (blocked if units of that design exist on the map)
- Design changes take effect immediately — newly built units use the updated designs

---

## Key Code Types

### Domain Types (Spx.Nexus.Domain)

| Type | Description |
|---|---|
| `NexusState` | Aggregate root: game ID, round number, systems, players, completion status, last resolve events |
| `NexusSystemState` | One system: coord, isNexus, homePlayerId, energyValue, supplyValue, controlOwner, units dictionary |
| `NexusPlayerState` | One player: faction, energy, gate progress, pending orders, designs |
| `HexCoord` | Axial hex coordinate (Q, R) with distance and neighbour helpers |
| `NexusUnitDesign` | A player-created design: name, hull category, module list |
| `NexusUnitModule` | Abstract base for all modules (Shield, Battery, Hangar, etc.) |
| `NexusUnitProfile` | Derived combat/move profile from design: hits, silhouette, attacks, cost, move, carry |
| `NexusUnitStack` | Runtime stack in a system: design ID, remaining hits, count, category |
| `NexusUnitStackGroup` | Immutable snapshot of a stack for events/views |
| `NexusMoveOrder` | A fleet move: source, waypoints, stacks selected |
| `NexusBuildOrder` | Build N units of a design |
| `NexusTurnOrdersCommand` | All orders for one player for one round |
| `NexusTurnOrdersResult` | Accepted or one of many typed rejection reasons |
| `NexusGameView` | Projected view for one player: systems, current player, opponent, resolve events |
| `NexusGameCompletion` | Game outcome: victory (winner) or draw |

### Engine (NexusEngine.cs)

| Method | Description |
|---|---|
| `Initialize` | Creates game state with default designs, 5 energy, system stat triples (Energy + Supply per system) |
| `SubmitOrders` | Validates and stores orders; when both submit, resolves the round |
| `BuildView` | Projects domain state into a player-specific view (hides opponent pending orders) |
| `CreateDesign` | Creates a new unit design with module validation |
| `DeleteDesign` | Soft-deletes a design (blocked if units exist on map) |
| `Abandon` | Marks a player inactive; if one remains, they win |

### Resolve Events (NexusResolveEvents.cs)

| Event | Description |
|---|---|
| `NexusUnitsMovedEvent` | Fleet moved between systems |
| `NexusPlanetaryControlEvent` | Player gained system control |
| `NexusSystemContestedEvent` | System became contested |
| `NexusSystemUncontrolledEvent` | System became uncontrolled |
| `NexusCombatResultEvent` | Complete combat result with phase results, loss lists, and all attack rolls |
| `NexusSystemClearedEvent` | One player eliminated from a system |
| `NexusIncomeEvent` | Player collected Energy income |
| `NexusUnitDeployedEvent` | Built units appeared at home system |
| `NexusCapitalDisbandedEvent` | Capital ship disbanded due to supply |
| `NexusGateStartedEvent` | Nexus Gate construction began |
| `NexusGateCompletedEvent` | Nexus Gate construction completed |
| `NexusGateCancelledEvent` | Nexus Gate construction cancelled |
| `NexusVictoryEvent` | Game won |
| `NexusDrawEvent` | Game ended in draw |

---

## Combat Flow (Detailed)

### Per-Contested-System Resolution

1. **Expand** both players' unit stacks into individual combat units (one per count)
2. **Contact phase** — each unit with Vanguard modules attacks eligible targets; all attacks resolve simultaneously; hits queued and applied after all dice rolled
   - All attacks collect pending hits; shield checks performed per hit
   - Destroyed units removed before Battle phase
3. **Battle phase** — each unit with Battery modules attacks eligible targets; same simultaneous resolution
4. **Collapse** surviving units back into stacks (grouped by design ID + remaining hits)
5. **Emit** `NexusCombatResultEvent` if any phase resolved
6. **Emit** `NexusSystemClearedEvent` if one side eliminated
7. **Update** system control (planetary units with Control module)

### Targeting Weights

- Each unit's silhouette determines its probability weight for being targeted
- Screen modules reduce the effective silhouette of covered friendly Capitals by 1 (min 1)
- Cloak reduces silhouette; Beacon and Bulkhead increase it
- The selection pool is restricted to units eligible for the attacker's current phase

### Shield Mechanics

- Shielded unit: on incoming hit, roll 1d6; 4+ = absorbed (shield consumed for this turn)
- Shield regenerates at end of each combat (after Battle phase)
- Disruptor module bypasses shields entirely

### Command Bonus

- Command(Category) module on a friendly unit reduces hit threshold by 1 for up to N same-category units
- Beneficiaries are the highest-silhouette units that are not themselves Command providers
- If the attacking unit is covered, GetCommandBonus returns 1, reducing effective threshold by 1
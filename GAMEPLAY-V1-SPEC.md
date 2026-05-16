# Gameplay V1 Spec

## Summary

Gameplay v1 replaces the current rock-paper-scissors loop with a 1v1 crafting game built around a shared visible pool, private hands, simultaneous play batches, and deterministic batch resolution.

This version is intentionally scoped smaller than a full deckbuilder:

- players have a hand, not a deck
- the shared pool is finite and visible
- each acquire phase has two acquire rounds, and each player acquires one card per round
- each player can play up to 3 cards per play phase
- match state must survive silo restarts

## Match Structure

- Format: 1v1
- Persistence: match state must survive restarts
- Visibility: no long-lived hidden state in v1; only locked play batches are hidden until both players commit
- Win condition: first player to produce `Victory` wins
- `Victory` is created by `Produce`
- The `Victory` recipe requires one of each resource and consumes them:
  - Red
  - Yellow
  - Blue
  - Purple
  - Green
  - Orange
- A player wins if they have `Victory` in hand at win check
- Simultaneous victory: if both players produce `Victory` in the same resolution, the match is a draw
- Timeline: gameplay events should be persisted to the game timeline

## Shared Pool

- The shared pool is finite and visible to both players
- The shared market deck contains only `Extract`, `Refine`, and `Produce` cards
- The initial shared market deck contains 24 total cards:
  - 12 `Extract`
  - 6 `Refine`
  - 6 `Produce`
- The visible market size is 5 cards
- Players take turns during the acquire phase
- Each acquire phase contains two acquire rounds
- In each acquire round, each player acquires exactly one card
- An acquired card is removed from the shared pool and added directly to the player's hand
- Played `Extract`, `Refine`, and `Produce` cards are recycled back into the shared market deck after resolution
- The shared market deck is shuffled before refilling the visible market back to 5
- The market refills back to 5 only after both players finish both acquire rounds for the phase
- If the market deck cannot refill to 5, reveal as many cards as remain
- If the market deck is empty, play continues with the remaining hands until a win or draw condition is reached
- At match start, both players begin with empty hands
- At match start, the market deck is shuffled and the first 5 market cards are revealed before the first acquire phase begins

## Phase Loop

Each round follows this phase structure:

1. Acquire phase
2. Play phase
3. Resolve phase
4. Win check

If no player has won, the next acquire phase begins.

## Acquire Phase

- Acquire pick order is determined at the start of each acquire phase
- The acquire phase contains two acquire rounds
- In each acquire round, the first picker chooses one card from the visible market and then the second picker chooses one card
- The same pick order is used for both acquire rounds in the same acquire phase
- Chosen cards go directly to hand
- No deck, discard pile, draw pile, or resource meter exists in v1
- Both acquire rounds use the same visible market snapshot for that phase, with no refill between rounds

### Acquire Initiative

- At the start of each acquire phase, each player's hand generates an initiative score
- The player with the lower initiative score picks first
- Initiative score is calculated from the cards currently in hand:
  - `Extract`, `Refine`, and `Produce` cards = 1 point each
  - base resource cards = 1 point each
  - refined resource cards = 2 points each
  - effect cards = 3 points each
- If both players have the same initiative score, the player with the lower total hand size picks first
- If both players are still tied, the player who picked second in the previous acquire phase picks first
- If a tie still exists in the first acquire phase, choose randomly
- `Scout` overrides initiative scoring for the next acquire phase only
- If only one player has an active `Scout`, that player picks first
- If both players have an active `Scout`, the `Scout` effects cancel and normal initiative rules apply

## Play Phase

- Both players choose cards from hand simultaneously
- A player may lock a batch of 0 to 3 specific card instances
- Locked cards are hidden until both players have committed
- Only locked cards are considered played this batch
- Cards created during resolution can never become newly played cards in the current batch

## Resolution Order

Played cards resolve in this strict order:

1. Effects
2. Extracts
3. Refines
4. Produces

This resolution order applies regardless of the order in which the player selected the cards.

## Strict Batch Membership

Batch membership is strict:

- only cards selected at lock time are played this batch
- newly created cards never join the current batch as played cards
- newly created cards may still be consumed by later played cards in the same batch if the resolution order makes them available
- locked cards leave the hand and enter a hidden pending batch zone when committed
- effects may target cards in a player's hand or cards in a pending batch when the effect text allows it

Examples:

- `Extract(Red) + Extract(Blue) + Refine` is valid because the selected `Refine` may consume the resources created by the selected `Extract` cards earlier in the batch
- `Extract + Refine + Produce` is valid if the selected `Produce` can consume resources created earlier in the same batch
- a newly produced Effect card cannot resolve in the same batch unless that Effect card was already one of the locked cards from hand

## Card Categories

V1 uses three card categories:

1. Action cards
2. Resource cards
3. Effect cards

### Action Cards

- `Extract`
- `Refine`
- `Produce`

### Resource Cards

Base resources:

- Red
- Yellow
- Blue

Refined resources:

- Purple
- Green
- Orange

### Effect Cards

- Effect cards are created by `Produce`
- Effect cards can be played later for effect
- The first six effect cards are:
  - `Sabotage`
  - `Replicate`
  - `Catalyst`
  - `Corrupt`
  - `Reclaim`
  - `Scout`

## Card Rules

### Extract

- `Extract` produces one base Resource card in a chosen color
- Valid extract colors are Red, Yellow, and Blue

### Refine

- `Refine` combines two base Resource cards into one refined Resource card
- Supported color combinations are:
  - Red + Blue = Purple
  - Blue + Yellow = Green
  - Red + Yellow = Orange

### Produce

- `Produce` consumes unrefined and refined Resource cards
- `Produce` creates a named card that can later be played for effect
- Produced cards may be consumed by later played cards in the same batch if a later played card requires them as ingredients
- Produced cards may not become newly played cards in the same batch
- `Produce` may also create `Victory` by consuming one of each resource
- The first six `Produce` recipes are:
  - `Sabotage` = Red + Yellow
  - `Replicate` = Blue + Yellow
  - `Catalyst` = Red + Blue
  - `Corrupt` = Orange + Blue
  - `Reclaim` = Green + Red
  - `Scout` = Purple + Yellow

### Effects

- Effect cards resolve in the first resolution step
- Newly produced Effect cards do not resolve in the same batch unless they were already locked from hand before resolution began
- The first six effect definitions are:
  - `Sabotage`: opponent discards one base resource card of your choice from their hand
  - `Replicate`: create one base resource card matching a base resource already in your hand
  - `Catalyst`: convert one base resource in your hand into another base color
  - `Corrupt`: opponent discards one refined resource card of your choice from their hand
  - `Reclaim`: choose one other played `Extract`, `Refine`, or `Produce` card in this batch; after resolution, return it to your hand instead of returning it to the market deck
  - `Scout`: override initiative and choose first at the start of your next acquire phase

## Validation Rules

Server-side validation should enforce the gameplay rules rather than relying on UI-only checks.

A submitted play batch is rejected when:

- it contains more than 3 selected cards
- any selected card is not in the player's hand at lock time
- a selected card requires player choices and those choices are invalid at lock time

Players must declare required choices at lock time for cards that need them.

## Resolution Failure Rules

The agreed v1 behavior is:

- player choices are specified at lock time
- the server validates that the locked batch is structurally valid at lock time
- if a selected card later cannot resolve because the game state changed during earlier resolution steps, it fizzles and is consumed

## Draw Rules

- If both players produce `Victory` in the same resolution, the match is a draw
- If both players pass the play phase for two consecutive rounds and no card entered or left either player's hand during those rounds, the match is a draw

## Card Lifecycle

- Played cards are always consumed when played
- Consumed played `Extract`, `Refine`, and `Produce` cards are returned to the shared market deck after resolution
- Consumed played Effect cards are destroyed and do not return to the market deck
- This recycle rule applies to played cards, not to resource cards consumed as ingredients by `Refine` or `Produce`
- Effects may override the default recycle destination for specific cards

## Open Design Decisions

These points are still intentionally unresolved:

- none currently locked in this spec

## Implementation Impact

This spec implies a replacement of the current RPS-shaped gameplay model with a phase-based session model.

The main implementation areas are expected to be:

- `src/Spx.Contracts`: new session contracts and command/query models
- `src/Spx.Grains`: session state machine and batch resolution engine
- `src/Spx.Game.Application`: outcome shapes and application handlers for acquire and play actions
- `src/Spx.Web`: game session UI for acquire, hand display, locked play batches, and resolved batch summaries
- timeline support for gameplay system events

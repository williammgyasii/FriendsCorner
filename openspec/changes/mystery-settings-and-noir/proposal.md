## Why

Murder Mystery plays well, but it looks plain and it plays one way. William wants the game master to choose how hard the case is and whether the two players work together or race, a hard case with a clock, a dramatic "final answer" moment when both accuse, and a mystery screen that looks and moves like a case file for the case being played. He also wants the whole app dark and the lobby's game tiles to look like cards.

This change builds on `add-murder-mystery`, which is finished but not yet archived; archive that one first.

## What Changes

- The lobby gets **mystery settings**. When the game master picks Murder Mystery, a panel opens with **Level** (Easy or Hard) and **Mode** (Together or Race). Guests see the choice; changing it clears their ready, like changing the game.
- **Easy**: 8 leads, no clock. **Hard**: 6 leads (still enough; the fairness check already needs at most 6), a clock the writer sets from the case (5–20 minutes), and a 50-point bonus for solving it.
- **Together** (today's mode) gains a **final answer**: when both pick the same suspect, a 5-second countdown starts; either player can hold on (withdraw) or switch, which cancels it. At zero the case is revealed.
- **Race**: each player has their own leads, sees only what they opened, and accuses once. Right wins; wrong is out. If both are out, or the clock runs out, nobody wins.
- The writer is told the level and returns two more fields: `minutes` (how long a good pair needs) and `mood` (one of six looks: frost, storm, velvet, garden, smoke, gilded).
- The mystery screen becomes a **noir case file**: a mood-coloured scene with a moving ambience (snow, rain, spotlights, fireflies, smoke, gold dust), a 3D case folder, dossier cards that tilt, evidence cards that flip open, a stamped accusation, a full-screen final-answer countdown, a stamped reveal, and a writing screen that keeps the wait lively.
- The whole app goes **dark**, and the lobby's game tiles become **playing cards**: portrait, small corner radius, framed, with corner pips.

## Capabilities

### New Capabilities

- `mystery-settings`: level and mode, chosen in the lobby and carried into the game.
- `app-look`: the dark theme and the lobby's card tiles.

### Modified Capabilities

- `murder-mystery`: levels, the Hard clock, the Together final answer, the Race mode, per-seat views.
- `murder-mystery-room`: the room's clock drives the game's timers; the writer is told the level and returns minutes and mood.
- `murder-mystery-screen`: the noir case-file screen.

## Impact

- Core: `LobbyEngine`, `RoomEngine`, `GameCatalog`, `IGameEngine` (a clock hook), `MysteryGame`, `MysteryState`, `MysteryView`, `Case`, `CaseRequest`, `RoomManager`.
- Infrastructure: `StateMessageAccessor` (lobby settings, per-seat mystery), `MysteryTableAccessor` (new saved fields), `OpenAiCaseWriterAccessor`, the prompt and schema.
- Api: `ClientMessageReader` (`mystery-settings`, `mystery-withdraw`).
- Frontend: `index.css` and `style.css` (dark tokens), `GameGrid`, a new `MysterySettings` lobby panel, `lobbyLook`, `mysteryLook`, `MysteryScreen`, the store.
- No new dependencies. No migration: the state is a jsonb column.

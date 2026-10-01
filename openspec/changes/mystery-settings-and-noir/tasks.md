## 1. Settings in the lobby (Core)

- [x] 1.1 Write failing tests: the lobby defaults to easy together; the host sets hard race; a guest and a counting-down lobby are refused; a change clears ready; a mystery launched from the room carries the lobby's settings, and a rematch keeps them.
- [x] 1.2 Add `MysteryLevel`, `MysteryMode`, `MysterySettings`, `SetMysterySettings`, `LobbyEngine.Mystery`/`TrySetMystery`, `GameStart`, and pass settings through `GameCatalog` and `RoomEngine.TryLaunch`.

## 2. Levels, clocks, final answer (Core)

- [x] 2.1 Write failing tests: hard has 6 leads; the hard clock starts on the first tick (minutes clamped to 5–20) and running out reveals as timed out; the hard bonus; together agreement enters accusing, the next tick sets the lock, a tick at the lock reveals; switching or withdrawing cancels; no lead opens while accusing; `RoomEngine.Advance` reports a game change when the game's clock moves it.
- [x] 2.2 Add `OpenedLead`, `Withdraw`, `MysteryPhase.Accusing`, `EndsAt`/`LocksAt`, `IGameEngine.TryAdvance`, `Case.Minutes`/`Mood`, and update the existing tests whose behaviour the spec changed.

## 3. Race (Core)

- [x] 3.1 Write failing tests: private budgets; a right accusation wins with the score; a wrong one puts you out (no more opens or accusations); both out reveals with no winner; the clock running out reveals with no winner; `View(seat)` hides the partner's leads and pick but shows who is out.
- [x] 3.2 Implement race and the per-seat view.

## 4. Room, writer, wire, storage

- [x] 4.1 Write failing tests: the manager's case request carries the level; the writer's input says `Difficulty: hard`, the schema requires `minutes` and `mood`, and a reply maps them; the lobby message carries the settings; the mystery message carries level, mode, mood, clocks, `by`, `out`, winner and timed-out, and hides race secrets per seat; the reader reads `mystery-settings` and `mystery-withdraw`; a hard race round-trips through the table, and an unreadable row loads nothing.
- [x] 4.2 Make them pass: `CaseRequest.Level`, the prompt and schema, `StateMessageAccessor`, `ClientMessageReader`, `MysteryTableAccessor`.

## 5. Lobby look (Frontend)

- [x] 5.1 Write failing tests: `describeLobby` gives a mystery panel only when mystery is picked (host can change, guest can't); the lobby screen's panel sends `mystery-settings`; each game card has two corner pips.
- [x] 5.2 Build `MysterySettings.tsx`, the card tiles, and the dark tokens across `index.css` and `style.css`.

## 6. Noir mystery screen (Frontend)

- [x] 6.1 Write failing tests for `describeMystery`: the accusing overlay (name, seconds), the hard clock (m:ss, warn under a minute), race hints and out text, the reveal headlines (together, race, timed out), lead `by`, and mood.
- [x] 6.2 Write failing screen tests: `data-mood`; writing lines; the final-answer overlay with "Hold on" sending `mystery-withdraw`; the clock.
- [x] 6.3 Build the noir screen: mood palettes and ambience, the 3D folder, dossier and flip cards, the stamp, the overlay, the reveal; reduced motion.

## 7. Check

- [ ] 7.1 Play in a real browser at 390 by 844 and 1440 by 900: the settings panel, hard race, and together with the final answer. No sideways scroll. Both suites and `npm run build` pass.
- [ ] 7.2 Walk William through it; leave commit, push, and archive to him.

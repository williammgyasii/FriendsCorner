## Context

Letter Tiles shipped with a working but cramped page (`frontend/src/tiles/TilesScreen.tsx`): a board, an aside with scores, and a fixed tray that covers the board's bottom row. The face videos live in `main.ts` and are moved into a game's `.player-face` hosts by `placeFaces`; Letter Tiles passes no hosts, so the faces disappear. Scores come only after Submit, from `Scorer` inside `LetterTilesState.TryPlay`.

The reference is Scrabble GO's board screen. We take its hierarchy (big numbers on top, quiet board, loud small premiums, chunky tiles, live score bubble) and leave its currency, power-ups, and timers.

## Goals / Non-Goals

**Goals:**

- The board is never covered and every label fits, on a 390 by 844 phone and a 1440 by 900 desktop.
- Faces sit on the players' badges, as in chess and tic-tac-toe.
- A player sees what a placement scores, or why it would be refused, before pressing Submit.
- Drag to place, with tap kept.

**Non-Goals:**

- Back to the lobby and rejoin (next change).
- Faces for seats C and D, keyboard play, animations beyond the rack lift, sounds.

## Decisions

### 1. The preview is a question, not a command

A preview reads the game and changes nothing, so it goes through a separate door from moves (command-query separation). `IGameEngine` gains:

```csharp
GameAnswer? Ask(Seat seat, GameQuestion question) => null;
```

It is a default interface method, so `TicTacToeGame` and `ChessGame` stay untouched. `GameQuestion` and `GameAnswer` are abstract records next to `GameMove`. `PreviewTiles(IReadOnlyList<PlacedTile>)` is a `GameQuestion`; `TilesPreview(Words, Score)` and `TilesPreviewRefused(Refusal)` are `GameAnswer`s, each carrying the asked tiles back.

`RoomManager.Act` checks for a question before `Apply`:

```
question ─► lock { answer = room.Ask(seat, q) } ─► answer? ─► sockets.Send(seat, state.Answer(answer))
                                                   (no Record, no Broadcast)
```

Alternatives considered:

- **Store the preview per seat in the state and broadcast it**, like refusals. Rejected because every drag would save the game and send a state message to every seat.
- **Copy the scorer into TypeScript.** Rejected because the rules would live in two places and drift. `Scorer` owns scoring.

### 2. One judge for play and preview

`LetterTilesState.TryPlay` today checks placement, finds words, checks the word list, and scores, then draws and turns. Extract the first half into a private `Judge(seat, placed, words)` that returns `(Words, Score)` or a `Refusal`. `TryPlay` calls `Judge` and then commits; `Preview` calls `Judge` and stops. This is a parallel change: the existing `TryPlay` tests stay green while `Judge` is pulled out, which guarantees "a preview matches the play". The turn check stays in `TryPlay`, so a waiting player can preview.

### 3. The wire

The client sends `tiles-preview` with the `tiles-play` tile shape, and `ClientMessageReader` reuses `Placed()`. `IStateMessageAccessor` gains `string Answer(GameAnswer answer)`, written by `StateMessageAccessor` as `{"type":"tiles-preview","tiles":[...],"words":[...],"score":N}` or with `refusal` in place of words and score. The kebab-case refusal codes are the same as the state message's.

### 4. The page asks, and ignores stale answers

`tilesUiSlice` exposes `askPreview()`, which sends the current unsent tiles if there are any. `TilesScreen` calls it from one effect keyed on the unsent tiles and the board, so a preview is re-asked both when you move a tile and when another player's play changes the board under it. The reducers drop the kept preview whenever the unsent tiles change. It sends immediately, with no debounce: previews are tiny, the socket is local, and the echo makes ordering safe. The answer is kept only if its tiles equal the current unsent tiles (same squares and letters), which is how "stale answer" is handled without request ids. `roomSocket.ts` routes `tiles-preview` messages to a `previewReceived` action. It must pass the whole answer through; the missing `tiles` field in `stateReceived` was the last bug here.

### 5. Drag with `motion`, drop by hit test

We already ship `motion`, and its `drag` gives pointer and touch dragging. On drag end, `document.elementsFromPoint(x, y)` finds the first element with `data-square` or `data-rack`, and a pure `dropTarget(elements)` function turns that into "square N", "rack", or "nowhere". The dragged tile snaps back visually with `dragSnapToOrigin`, and the store moves it. Tap-then-tap stays for small screens and screen readers.

Alternative: **dnd-kit** has built-in keyboard sensors and accessible announcements, but it adds a dependency. We keep tap as the accessible path for now and revisit dnd-kit if keyboard play is added.

### 6. Layout with grid areas and container queries

`TilesScreen` becomes a CSS grid with named areas:

```
phone (portrait)        desktop (min-width 1024px and landscape)
┌───────────────┐       ┌────────┬──────────────┬────────┐
│ players       │       │players │    board     │ plays  │
│ board         │       │ status │              │ bag    │
│ rack          │       │        ├──────────────┤        │
│ actions       │       │        │ rack actions │        │
└───────────────┘       └────────┴──────────────┴────────┘
```

The board cell is a size container (`container-type: size`). The board is `min(100cqw, 100cqh)` square, and tile letters and premium labels use `cqi` units, so they scale with the board and never clip. Nothing is `position: fixed` over the board.

### 7. Faces in badges

`TilesScreen` renders two empty `.player-face` hosts, `data-face="you"` in your badge and `data-face="partner"` in your call partner's (A and B only; C and D get their letter). A small `faceHosts(world, root)` function in `src/faceHosts.ts` returns the two hosts for any game world. `main.ts`'s `placeFaces` uses it and is re-run on each render, since React may commit after `showWorld`; `placeFaces` already returns early when the videos are in place.

### 8. Look

We use Friends Corner's tokens (`--background`, `--card`, `--primary`, Fredoka) and give premiums the reference's contrast:

- Plain squares are `#efe9e1`, premium chips are TW `#e4572e`, DW `#f08a9c`, TL `#1f8fa3`, DL `#8fc3ea`, and the star is on DW.
- Tiles are cream `#fbe7b5` with a `#d9ae52` bottom bevel, and a newly placed (unsent) tile has a `--primary` ring.
- The preview bubble is a small green `#16a34a` pill at the end of the main word with the score, or a red pill with the reason.
- The players strip has one rounded card per seat: a face or letter, a large score, and "N tiles". The seat to move gets a thick ring in its seat colour.

## Risks / Trade-offs

- **jsdom cannot measure layout**, so the phone and desktop scenarios are checked in a real browser at both sizes. A bounding-box script asserts that everything is visible and nothing overlaps, with a screenshot of each size.
- **Preview traffic**: one small message per tile move, answered to one seat. It is cheap for 2 to 4 people. If it ever matters, add a 100 ms debounce on the page.
- **Hit-testing under the dragged tile**: `elementsFromPoint` returns the dragged element first, so `dropTarget` must skip it (it is marked `data-dragging`).
- **Face timing**: if React has not rendered the hosts yet, `placeFaces` leaves the videos in the dock until the next render, and they are never lost.

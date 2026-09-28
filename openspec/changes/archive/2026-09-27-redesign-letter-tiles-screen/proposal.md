## Why

The first Letter Tiles page works but is hard to play. On a laptop-sized window the tile tray covers the board's bottom row, premium labels (`TW`, `DW`) are clipped, and the score panel falls below the fold, so nobody can see whose turn it is. It also drops the face call the other games keep next to the board, which is the point of Friends Corner. The reference is Scrabble GO's board screen: a bold header with big numbers, a quiet board with small loud premium chips, chunky tiles, and a live score bubble while you place a word.

## What Changes

- **Layout for phone and desktop:** one board that is always as large as the smaller of the free width and height, and nothing drawn over it.
  - Phone (portrait): players strip on top, then the board at full width, then the rack and an action bar in thumb reach.
  - Desktop (wide): players and faces on the left, the board in the middle with the rack and action bar under it, last plays and the bag on the right.
- **Players strip with faces:** each playing seat gets a badge with its score, tile count, and a ring when it is to move. Your badge and your call partner's badge hold the live faces, like chess and tic-tac-toe. A status line says "Your turn", "B is thinking", or the ending.
- **Look:** Friends Corner's warm palette at the reference's contrast: pale squares, small saturated premium chips whose labels scale with the square, chunky cream tiles with a bevel and a small point value.
- **Placing tiles:** drag a rack tile onto a square, drag an unsent tile to another square or back to the rack. Tap a tile, then a square, still works. A Shuffle button reorders your rack on your page only.
- **Live score preview:** while unsent tiles are on the board, the page asks the server what they would score. The unsent tiles get an outline and a bubble with the score, and Submit shows it ("Submit 12"). If the placement is illegal or makes an unknown word, the bubble says so before Submit.
- **Preview on the server:** a new `tiles-preview` question. The room answers only the seat that asked, never saves, never broadcasts, and never changes the turn. The words and score come from the same `Placement`, `WordFinder`, and `Scorer` a real play uses.

Stays later (not in this change):

- Going back to the lobby from a game and rejoining it (its own change next).
- Faces for seats C and D. The face call stays A with B; C and D get a lettered badge.
- Tile animations beyond the rack lift, sounds, turn timers, hints.
- Keyboard play (typing letters onto the board).

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `letter-tiles-screen`: the board and rack fit phone and desktop without overlap, placing tiles can be done by drag, the score panel becomes a players strip with faces and a status line, and a live score preview is added.
- `letter-tiles`: a placement can be scored without playing it.
- `letter-tiles-room`: a `tiles-preview` question answered to the asking seat only.

## Impact

- **Core:** `IGameEngine` gains a question door (`Ask`) with a default of no answer, so tic-tac-toe and chess do not change. `LetterTilesState.Preview` reuses the play path without drawing, turning, or refusing into state. `RoomManager` sends an answer to one seat without recording or broadcasting.
- **Infrastructure:** `StateMessageAccessor` (or a sibling writer) writes a `tiles-preview` answer message.
- **Api:** `ClientMessageReader` reads `tiles-preview` with the same tile shape as `tiles-play`.
- **Frontend:** `TilesScreen.tsx` rebuilt into players strip, board, rack, and action bar components; drag with the existing `motion` library; `tilesUiSlice` gains rack order, drag, and the latest preview; `tilesLook.ts` gains the status line and the preview text; `main.ts` places the face videos in the tiles badges; CSS container queries for the board.
- **No database or wire change** for state messages; the `tiles` section is unchanged.

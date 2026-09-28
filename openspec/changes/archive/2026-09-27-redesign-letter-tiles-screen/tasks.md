## 1. Preview in the rules (Core)

- [x] 1.1 Write failing tests in `LetterTilesGameTests` for `LetterTilesState.Preview(seat, placed, words)`: seat B previews `CAT` across 111–113 while A is to move and gets `["CAT"]`, 10; `QXZ` answers `not-a-word` naming `QXZ` and stores no refusal; 111 and 113 answers `gap`; a finished game answers `game-over`; and for a mid-game placement, preview then `TryPlay` give the same words and score. Assert the board, racks, bag, scores, turn, and scoreless count are unchanged after each preview.
- [x] 1.2 Extract `Judge` from `TryPlay` with every existing Letter Tiles test green, then add `Preview` on top of it.

## 2. The question door (seam)

- [x] 2.1 Write failing tests in `GameSeamTests`: `TicTacToeGame` and `ChessGame` answer `null` to any question; `LetterTilesGame.Ask(seat, PreviewTiles)` answers `TilesPreview` or `TilesPreviewRefused` with the asked tiles echoed; `RoomEngine.Ask` answers `null` when no game is running.
- [x] 2.2 Add `GameQuestion`, `GameAnswer`, a default `IGameEngine.Ask`, `PreviewTiles`, the two answers, `LetterTilesGame.Ask`, and `RoomEngine.Ask`.

## 3. The room and the wire

- [x] 3.1 Write failing tests:
  - `ClientMessageReaderTests`: `tiles-preview` becomes `PreviewTiles`, and a bad square is ignored.
  - `StateMessageAccessorTests` (golden JSON): a `TilesPreview` and a `TilesPreviewRefused` answer.
  - `RoomManagerTests`: a question sends one message to the asking seat, notes nothing to the recorder, and sends nothing to the other seat; a `null` answer sends nothing.
- [x] 3.2 Implement the reader case, `IStateMessageAccessor.Answer`, and the question branch in `RoomManager.Act`.

## 4. Page state

- [x] 4.1 Write failing tests in `tilesUi.test.ts` and `roomSocket.test.ts`:
  - `askPreview()` sends `tiles-preview` with the current unsent tiles and sends nothing when there are none. Changing the unsent tiles drops the kept preview. The screen calls `askPreview()` when the unsent tiles or the board change (tested in 6.1).
  - A `tiles-preview` socket message reaches the store; an answer whose tiles equal the unsent tiles is kept, and a stale one is ignored.
  - `moveUnsent(from, to)` moves an unsent tile.
  - `returnToRack(square)` puts it back.
  - `shuffleRack()` with an injected random reorders the rack view, keeps the same tiles, and sends nothing.
- [x] 4.2 Implement the slice actions, the preview thunk, the `previewReceived` reducer, and the socket routing.

## 5. The look

- [x] 5.1 Write failing tests in `tilesLook.test.ts`:
  - The status line reads "Your turn", "B is thinking", or the ending.
  - The badges mark `face: 'you' | 'partner' | null` (the partner only for A with B).
  - The preview bubble reads `10`, or "QXZ is not in the word list" for a refusal, or nothing without unsent tiles.
  - The Submit label is "Submit 10" with a preview and "Submit" without one.
  - `dropTarget` turns an element list into square N, rack, or nowhere, skipping the dragged element.
- [x] 5.2 Implement them in `tilesLook.ts` (and `dropTarget` beside it).

## 6. The screen

- [x] 6.1 Write failing tests in `tilesScreen.test.tsx`:
  - The players strip lists every seat's badge with its score and tile count, and rings the seat to move.
  - The status line is shown.
  - Your badge and your partner's badge contain `[data-face]` hosts.
  - The bubble and "Submit 10" appear for a kept preview.
  - The Shuffle button reorders the rack and sends nothing.
  - Last plays and the bag are shown.
- [x] 6.2 Rebuild `TilesScreen` into `PlayersStrip`, `Board`, `Rack`, `ActionBar`, and `PlaysPanel`, with motion drag and the grid-area and container-query CSS. Remove the fixed tray.

## 7. Faces in the badges

- [x] 7.1 Write a failing test for `faceHosts(world, root)`: it returns the two `.player-face` hosts for `tiles` (you, then partner), the chess and tic-tac-toe hosts for those worlds, and `null` for the lobby.
- [x] 7.2 Add `src/faceHosts.ts`, use it in `placeFaces`, and re-run `placeFaces` on each render.

## 8. End to end

- [x] 8.1 In a real browser with a scripted second seat, check at 390 by 844 and at 1440 by 900 that all 225 squares, the players strip, the rack, and Submit are inside the viewport with no overlapping bounding boxes. Take a screenshot of each. Place a real word and check that the bubble and "Submit N" match the score after Submit. Run the whole backend and frontend suites and `npm run build`.
- [x] 8.2 Walk William through the code, and leave commit and archive for his approval.

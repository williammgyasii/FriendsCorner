# Tasks

Every behavior task is a pair: write the failing test(s), run them, show red, stop for review; then write the minimal code, run the whole suite, show green.

## 1. Game seam (parallel change, no behavior change)

- [x] 1.1 Write a failing test `A_game_move_reaches_the_running_game_through_the_seam`: launch chess, apply `MoveChess e2-e4`, and assert `room.Game` is a chess game with seat B to move. Run it and confirm it fails to compile or fails.
- [x] 1.2 Expand: add `IGameEngine`, `GameMove`, `GameCatalog`, `TicTacToeGame`, `ChessGame`; `RoomEngine` holds `Game`, and `TicTacToe`, `Chess`, `TryPlace`, `TryChessMove`, `TryRematch`, `TryChessRematch`, `Restore…` become one-line pass-throughs. Confirm the new test and all 135 existing tests pass unchanged.
- [x] 1.3 Write failing `RoomManagerTests`: an accepted game move notes the running game once through a fake `IGameRecorderAccessor`, and `Restore(game)` makes the room run that game. Run and confirm red.
- [x] 1.4 Migrate `RoomManager` to `IGameRecorderAccessor` and `Restore(IGameEngine)`; add Infrastructure `GameRecorderAccessor` dispatching to the board and chess recorders; update DI. Confirm the whole suite and the container wiring test pass.
- [x] 1.5 Write failing `RoomRegistryManagerTests` against a fake `IGameTableAccessor` returning a chess game for a saved id. Run and confirm red.
- [x] 1.6 Migrate `RoomRegistryManager` to `IGameTableAccessor`; add Infrastructure `GameTableAccessor` over the board and chess tables. Confirm the suite passes.
- [x] 1.7 Migrate `StateMessageAccessor` to read `room.Game`. Confirm the golden JSON tests pass unchanged.
- [x] 1.8 Contract: move remaining tests to `room.Game`, delete the pass-throughs, replace `RoomChange.TicTacToe` / `RoomChange.Chess` with `RoomChange.Game`, map `chess-rematch` to `Rematch`. Confirm the suite passes and `RoomEngine` names no specific game (`rg "TicTacToe|Chess" RoomEngine.cs` finds nothing).

## 2. Tiles and layout

- [x] 2.1 Write failing tests: a fresh bag holds 100 tiles, 12 E, 2 blanks, total value 187; each letter's value matches the spec; two games with `new Random(7)` draw identical racks; the layout has 8 / 17 / 12 / 24 premiums, the landmark squares from the spec, and is symmetric under rotation and mirroring. Run and confirm red.
- [x] 2.2 Implement `Tile`, `TileSet`, `PremiumLayout`, and the shuffled bag. Confirm green.

## 3. Placement

- [x] 3.1 Write failing tests for every placement scenario in the `letter-tiles` spec: first play off centre, one tile on the first play, not in one line, a gap, floating, a tile not on the rack, and `CAT` on 111-113 accepted. Each refusal asserts its reason code. Run and confirm red.
- [x] 3.2 Implement `Placement`. Confirm green.

## 4. Words and scoring

- [x] 4.1 Write failing tests with a fake word list: `CAT` forms `[CAT]`; `S` on 114 forms `CATS`; `X` on 128 forms `TX` and is refused naming `TX`; `QXZ` is refused naming `QXZ`; `CAT` scores 10, `CATS` scores 6, `C`-blank-`T` scores 8, and a 7-tile play adds 50. Run and confirm red.
- [x] 4.2 Implement `WordFinder` and `Scorer`. Confirm green.

## 5. Turns and the end

- [x] 5.1 Write failing `LetterTilesGame` tests: a three-player start (7 each, bag 79, A to move); out of turn refused; refill after a 3-tile play (bag 50 to 47); exchange with bag 20 and refused with bag 6; pass; six scoreless turns end the game and subtract racks; going out with B holding `Q E` moves 11 each way; a 210-210-150 tie has two winners; rematch refused mid-game and rotating the starter after the end; refusal kept for the seat until its next accepted command. Run and confirm red.
- [x] 5.2 Implement `LetterTilesState` and `LetterTilesGame`, and register `tiles` in `GameCatalog`. Confirm green.

## 6. The word list

- [x] 6.1 Add ENABLE as `FriendsCorner.Infrastructure/Resources/enable1.txt` (embedded resource) with a `LICENSE.md` noting its public-domain source. Verify the file has about 172,800 lines and contains `cat`.
- [x] 6.2 Write failing `WordListAccessorTests`: contains `cat` and `CATS`, does not contain `qxz` or an empty string. Run and confirm red.
- [x] 6.3 Implement `WordListAccessor` (lazy, case-insensitive set) and register it as a singleton. Confirm green and the container wiring test passes.

## 7. The room and the wire

- [x] 7.1 Write failing tests: the lobby lists `tiles` for 4 players; a four-seat room that starts `tiles` has world `tiles`, all four playing, A to move; `ClientMessageReader` reads `tiles-play`, `tiles-exchange` (with `?` for a blank), `tiles-pass`, `tiles-rematch`, and ignores a play with a non-number square. Run and confirm red.
- [x] 7.2 Implement the lobby entry, the moves, and the reader. Confirm green.
- [x] 7.3 Write failing `StateMessageAccessorTests`: in a two-player game each seat's message lists only its own rack and the other's count; neither contains the other's letters or the bag order; a refusal appears only in the sender's message; tic-tac-toe and chess golden messages gain only `"tiles":null`. Run and confirm red.
- [x] 7.4 Implement the `tiles` section. Confirm green.

## 8. Save and restore

- [x] 8.1 Write failing Neon tests: a Letter Tiles state round-trips through `LetterTilesTableAccessor` with the same board, bag order, racks, scores, turn, and scoreless count; `GameTableAccessor` returns a saved Letter Tiles game; the migration test still reports nothing pending. Run and confirm red.
- [x] 8.2 Implement `LetterTilesRow`, the `AddLetterTiles` migration, the table accessor, the recorder, the dispatch in `GameRecorderAccessor` / `GameTableAccessor`, and DI. Confirm the whole suite passes against Neon.

## 9. The page

- [x] 9.1 Write failing Vitest tests for `tilesLook`: premium labels for squares 0, 3, 20, 112; Submit, Exchange, Pass enabled only on your turn, Exchange disabled with bag 5; the panel reads "A played CAT for 10"; the refusal text "QXZ is not in the word list"; the end text "B wins, 212 to 180". Run and confirm red.
- [x] 9.2 Implement `tilesLook` and the `tiles` type on the room snapshot (unchanged tiles keep their object). Confirm green.
- [x] 9.3 Write failing tests for the `tilesUi` slice: select then place, tap to take back, Recall, blank asks for a letter, the Submit payload matches the `letter-tiles-room` spec, and unsent tiles stay after a refusal. Run and confirm red.
- [x] 9.4 Implement the `tilesUi` slice and the send actions. Confirm green.
- [x] 9.5 Write a failing component test: `TilesScreen` renders 225 squares with the centre star and the player's 7 rack tiles, and never renders another seat's letters. Run and confirm red.
- [x] 9.6 Implement `TilesScreen`, mount it for world `tiles`, and add the lobby card. Confirm `npm test` and `npm run build` pass.

## 10. End to end

- [x] 10.1 Extend the live two-player script: pick `tiles`, start, A plays `CAT` (A 10, B to move), A's next try `QXZ` is refused only for A, each seat's message never has the other's letters, restart the backend and reopen the room with the same racks and turn. Run it against the dev servers and confirm every check passes.
- [x] 10.2 Update `openspec/config.yaml` context with the game seam and Letter Tiles, walk William through the code, and leave commit and archive for his approval.

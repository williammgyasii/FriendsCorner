## Why

Friends in a room can only play two-player games today, and the lobby already seats up to four. Letter Tiles is a crossword tile game for 2 to 4 players that fits a face call: slow turns, lots of talking. It is also the third game, and each game so far was wired into `RoomEngine`, the room messages, the recorder, and the page by hand. Adding a third that way makes the cost of every future game grow; extracting one game seam first keeps it flat.

## What Changes

- **Game seam (refactor, no behavior change):** add `IGameEngine` so `RoomEngine` holds one running game instead of one property per game. Move tic-tac-toe and chess behind it with parallel change, keeping every existing test and the golden state JSON green at each step. Per-game `RoomChange.TicTacToe` / `RoomChange.Chess` become one `RoomChange.Game`.
- **Letter Tiles rules (Core):** 15 by 15 board, 100-tile bag with two blanks, 7-tile racks, placement rules, main word plus cross-words, letter values, premium squares counted once, a 50-point bonus for using all 7 tiles, exchange, pass, and the end-of-game rack adjustment.
- **Word check:** every word a play forms must be in the ENABLE word list (public domain, about 173,000 words). A play with any unknown word is refused and the player keeps their turn.
- **Hidden information:** each seat's state message shows only that seat's own rack. Other seats see only how many tiles a player holds. The bag order never leaves the server.
- **Lobby:** a "Letter Tiles" card, 2 to 4 players; everyone seated in the room plays.
- **Save and restore:** a live Letter Tiles game survives a server restart like chess does, stored in a new `letter_tiles` table through an EF Core migration.
- **Page:** a board, your rack, tap a tile then tap a square to place it, Recall, Submit, Exchange, Pass, and a score panel.

Stays later (not in this change):

- Challenges (playing any word and letting others dispute it). Words are checked on submit instead.
- Drag and drop, tile animations, turn timers.
- Faces for seats C and D. The face call stays A with B.
- Moving tic-tac-toe and chess into one shared game-snapshot table.
- Word lists for other languages.

## Capabilities

### New Capabilities

- `game-seam`: every game plugs into the room the same way; existing tic-tac-toe and chess behavior is preserved.
- `letter-tiles`: the rules of Letter Tiles: bag, racks, placement, words, scoring, turns, end of game, and the word check.
- `letter-tiles-room`: how Letter Tiles runs in a room: lobby card, who plays, per-seat hidden state, commands, save and restore.
- `letter-tiles-screen`: what the page shows and lets a player do during a Letter Tiles game.

### Modified Capabilities

None. There are no main specs yet.

## Impact

- **Core:** `RoomEngine`, `RoomCommand` / `RoomChange`, `LobbyEngine.Games`, new `Engines/Games/` and `Engines/LetterTiles/`, a new `IWordListAccessor` and a Letter Tiles table and recorder interface.
- **Infrastructure:** `StateMessageAccessor` gains a per-seat `tiles` section; a word-list accessor that loads ENABLE from an embedded resource; a Letter Tiles table accessor, recorder, EF row, and migration; DI registration.
- **Api:** `ClientMessageReader` reads `tiles-play`, `tiles-exchange`, `tiles-pass`, `tiles-rematch`.
- **Managers:** `RoomManager` records through the running game; `RoomRegistryManager` restores a saved Letter Tiles game.
- **Frontend:** a game card, a `tiles` slice of the room snapshot, a pure `tilesLook`, a local placement slice, and a Letter Tiles screen.
- **Data:** a new `letter_tiles` table (room id, jsonb state). Existing tables are unchanged.
- **Dependencies:** the ENABLE word list file (about 1.8 MB, public domain) added to Infrastructure. No new packages.
- **Naming:** the product name is "Letter Tiles", not a trademarked game name.

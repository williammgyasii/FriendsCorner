## Context

See proposal.md for why. Today `RoomEngine` owns the room floor, the lobby, and each game as its own property (`TicTacToe`, `Chess`) with its own `Restore…` and `Try…` methods, and `RoomChange` has one flag per game. Four places switch on the game by hand: `RoomEngine.Apply` / `Advance`, `RoomManager.Record`, `RoomRegistryManager.Find`, and `StateMessageAccessor.Write`. Tests call the per-game members directly (`room.Chess`, `room.TryChessMove`), about 50 call sites.

Constraints: Core has no web, socket, JSON, or database references. `RoomManager` holds one lock (the gate) around every engine call and broadcasts each seat its own message. The clock ticks every 50 ms and broadcasts on every tick while the room is live. Existing game boards (`Board`, `ChessBoard`) are immutable values with `TryX(..., out updated)`.

## Goals / Non-Goals

**Goals:**
- One seam every game plugs into; a fourth game after this touches one engine class, one catalog line, one view section, and its own persistence.
- Letter Tiles rules that are pure and tested with no host, socket, or database.
- Hidden information enforced on the server, not the page.

**Non-Goals:**
- One shared snapshot table for all games (tic-tac-toe and chess keep their tables).
- A generic, reflection-based JSON view for games. Each game's view is written by hand.
- Seats C and D in the face call.

## Decisions

### 1. The seam covers rules, not views or storage

```
Core/Engines/Games/
  IGameEngine          Id, TryPlay(Seat, GameMove) -> bool, TryRematch() -> bool
  GameMove             abstract record; Place, MoveChess, PlayTiles, ExchangeTiles, PassTurn derive from it
  GameCatalog          id -> (IReadOnlyList<Seat> playing) -> IGameEngine
  TicTacToeGame        wraps Board
  ChessGame            wraps ChessBoard
Core/Engines/LetterTiles/
  LetterTilesGame      IGameEngine; holds the current LetterTilesState and each seat's last refusal
```

`RoomEngine` keeps one `IGameEngine? Game`. `Apply` routes any `GameMove` to `Game.TryPlay` and a single `Rematch` to `Game.TryRematch` (the page's `chess-rematch` and `tiles-rematch` both read as `Rematch`). `RoomChange.TicTacToe` and `RoomChange.Chess` become `RoomChange.Game`.

Views and storage stay outside the seam because they change for different reasons: a new field on the page is not a rules change, and a storage change is not either. Alternative considered: an `IGameEngine.ToJson()` method. Rejected because it pulls JSON into Core, which breaks the dependency rule.

### 2. Parallel change for the refactor

Expand (add the seam; old members become one-line pass-throughs to `Game`), migrate (callers one at a time: `RoomManager`, `RoomRegistryManager`, `StateMessageAccessor`, then tests), contract (delete pass-throughs and per-game flags). The suite stays green after every step. Alternative: rewrite in one step. Rejected: ~50 tests would stop compiling at once and the safety net is gone when it matters.

### 3. One recorder and one table door per room, dispatch in Infrastructure

`RoomManager` gets `IGameRecorderAccessor.Note(roomId, IGameEngine)` instead of one recorder per game, and `IRoomManager.Restore(IGameEngine)` instead of one restore per game. `RoomRegistryManager` gets `IGameTableAccessor.Load(roomId) -> IGameEngine?`. The Infrastructure implementations switch on the game type and call the existing board, chess, and new Letter Tiles tables and recorders. The per-game switch lives in exactly one layer (Infrastructure), and Managers stay free of game names.

### 4. Letter Tiles state is an immutable value; the game object holds it

`LetterTilesState` is an immutable record: `Board` (225 cells of `Tile?`), `Bag` (ordered list of `Tile`), `Racks` (seat -> tiles), `Scores`, `Playing` (seat order), `ToMove`, `Starter`, `ScorelessTurns`, `LastPlay`, `Outcome`. Rules are functions that return a new state or a `Refusal`. `Tile` is a record struct (`Letter`, `IsBlank`); a blank on the board carries the letter it stands for.

The rules split by what each checks:

| Type | Owns |
|---|---|
| `TileSet` | the 100-tile distribution and letter values |
| `PremiumLayout` | the 225-square layout below |
| `Placement` | one line, no gaps, centre on first play, at least 2 tiles first, connected later |
| `WordFinder` | main word and cross-words of length 2 or more |
| `Scorer` | letter and word premiums on new tiles only, blank = 0, +50 for 7 tiles |
| `LetterTilesState` | turns, refill, exchange, pass, scoreless count, going out, settlement, rematch |

Layout (row 0 at the top; `T` triple word, `D` double word, `*` centre double word, `t` triple letter, `d` double letter, `.` plain):

```
T..d...T...d..T
.D...t...t...D.
..D...d.d...D..
d..D...d...D..d
....D.....D....
.t...t...t...t.
..d...d.d...d..
T..d...*...d..T
..d...d.d...d..
.t...t...t...t.
....D.....D....
d..D...d...D..d
..D...d.d...D..
.D...t...t...D.
T..d...T...d..T
```

The same 225 characters are sent to the page as `layout`, so the engine is the only owner of where the premiums are.

### 5. Randomness is injected

`GameCatalog` creates `LetterTilesGame` with a `Random`. Production uses `Random.Shared`-seeded instances; tests pass `new Random(seed)` so draws repeat. The bag order is stored, so a restored game does not need the seed. Alternative: a hand-written shuffle interface. Rejected as a shallow wrapper around `Random.Shuffle`.

### 6. The word list is an accessor

Core declares `IWordListAccessor.Contains(string word)`. Infrastructure's `WordListAccessor` loads ENABLE (`Resources/enable1.txt`, embedded in the assembly) once, lazily, into a case-insensitive `HashSet<string>`, registered as a singleton. Engine tests use a small fake list. Alternative: load the file inside Core. Rejected: files are a resource, and Core stays free of I/O.

### 7. Refusals travel with the game, delivered on the next tick

`TryPlay` returns false for a refused command, and `LetterTilesGame` keeps that seat's `Refusal` (reason, unknown words) until the seat's next accepted command. The seat sees it on the next broadcast, which is at most one 50 ms tick away, so `RoomChange` needs no new "explain" value. Only that seat's message includes it.

### 8. Per-seat view in `StateMessageAccessor`

A new `tiles` section is built from `LetterTilesGame` for the receiving seat: `rack` is that seat's tiles only (empty when not playing), other seats appear as `count`. The bag appears only as a number. Tic-tac-toe and chess keep their sections; every message gains `"tiles": null` when the room is not running Letter Tiles.

### 9. Storage is one jsonb row per room

`letter_tiles(room_id text primary key, state jsonb not null)` via an EF Core migration. `LetterTilesTableAccessor` maps `LetterTilesState` to a storage DTO serialized with System.Text.Json. A jsonb blob fits because the state is always read and written whole, never queried by field. Alternative: normalized tables for board cells and racks. Rejected: more joins for no query that needs them.

### 10. The page keeps unsent tiles locally

A `tilesUi` slice holds the selected rack tile, unsent placements, the blank-letter prompt, and exchange selection. The server snapshot stays the only truth for the board and rack; unsent tiles are drawn on top. A pure `tilesLook` turns the snapshot plus `tilesUi` into what to draw and which buttons are enabled, and is unit tested. `TilesScreen` is a React component mounted when the world is `tiles`, like the lobby.

## Risks / Trade-offs

- [ENABLE misses some modern words and accepts some offensive ones] → Accepted for v1; the word list is behind an accessor so it can be swapped.
- [Word list costs memory, about 10 MB as a set] → Loaded once, lazily, on the first Letter Tiles check.
- [A bug in the per-seat view leaks another rack] → A test asserts one seat's message contains none of another seat's letters and no bag order.
- [Broadcast size grows at 20 per second] → About 3 KB per seat per tick; acceptable for 4 seats. Revisit if the room gets heavier.
- [Refusal appears up to 50 ms late] → Below what a person notices.
- [Parallel change leaves temporary duplicate members] → They are deleted in the contract step, and the task list ends that group with the deletion.

## Migration Plan

One new EF Core migration adds `letter_tiles`. Existing tables are untouched. It is applied at startup like the first migration. Rollback: the migration's Down drops `letter_tiles`; older builds ignore the table.

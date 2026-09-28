## Purpose

How Letter Tiles runs inside a room: picking it in the lobby, who plays, what each seat may see, the commands the page sends, and surviving a server restart.

## ADDED Requirements

### Requirement: Letter Tiles is a lobby game for everyone seated

The lobby MUST offer a game with id `tiles` for 2 to 4 players. When it starts, every seat in the room MUST play, in join order.

#### Scenario: A four-person room

- **WHEN** the host of a four-seat room picks `tiles`, everyone is ready, and the countdown finishes
- **THEN** the room's world is `tiles` and seats A, B, C, and D all play, with A to move

### Requirement: Each seat sees only its own rack

Every state message for a Letter Tiles room MUST include a `tiles` section with: the premium `layout` (225 characters), the `board` (225 entries, each a letter or empty, a blank shown in lower case), the letter `values`, each playing seat's `score` and tile `count`, the seat `toMove`, the number of tiles left in the `bag`, the `lastPlay` (seat, words, score) or none, and the `outcome` (winners and final scores) or none. The `rack` MUST list the receiving seat's own tiles and MUST be empty for a seat that is not playing. A message MUST NOT contain another seat's tiles or the order of the bag.

#### Scenario: Two players look at the same game

- **WHEN** seat A holds `A E I O U R S` and seat B holds `B C D F G H J`
- **THEN** seat A's message lists A's seven tiles and shows B's count as 7, seat B's message lists B's seven tiles and shows A's count as 7, and neither message contains the other's letters or the bag order

### Requirement: Commands from the page

The room MUST accept these messages from a playing seat: `tiles-play` with `tiles` (each a `square` number and a `letter`, with `blank: true` for a blank), `tiles-exchange` with `letters` (a blank is `?`), `tiles-pass`, and `tiles-rematch`. A malformed message MUST be ignored.

#### Scenario: A play arrives

- **WHEN** seat A sends `{"type":"tiles-play","tiles":[{"square":111,"letter":"C"},{"square":112,"letter":"A"},{"square":113,"letter":"T"}]}` as the first play
- **THEN** every seat's next message shows `CAT` on squares 111 to 113, A's score as 10, and B to move

#### Scenario: A malformed play

- **WHEN** a `tiles-play` message has a square of `"x"`
- **THEN** it is ignored and the room is unchanged

### Requirement: A refused command is explained to its sender only

When a play, exchange, or pass is refused, the next message to that seat MUST include a `refusal` with a reason (`not-your-turn`, `not-in-rack`, `not-in-line`, `gap`, `first-must-cover-centre`, `first-needs-two-tiles`, `not-connected`, `not-a-word`, `bag-too-small`, or `game-over`) and, for `not-a-word`, the unknown words. Other seats MUST NOT receive it. The refusal MUST clear on that seat's next accepted command.

#### Scenario: An unknown word

- **WHEN** seat A plays `QXZ` across the centre
- **THEN** seat A's next message has refusal `not-a-word` with words `["QXZ"]`, seat B's message has no refusal, and the board is unchanged

### Requirement: A Letter Tiles game survives a restart

After every accepted command, the room MUST save the whole game: board, bag order, racks, scores, turn, scoreless-turn count, and outcome. When a saved room link is opened after a server restart, the room MUST reopen the same game with the same racks and the same seat to move.

#### Scenario: Restart mid-game

- **WHEN** seat A has played `CAT`, the server restarts, and both players reopen the room link
- **THEN** `CAT` is on the board, both racks and scores are as they were, and seat B is to move

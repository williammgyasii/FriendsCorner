## MODIFIED Requirements

### Requirement: The board and the rack are drawn

The page MUST draw the 15 by 15 board with each premium square labelled (`DL`, `TL`, `DW`, `TW`, and a star on the centre), every tile on the board with its letter and point value (a blank shows its letter and no value), and the player's own rack with each tile's letter and value. The board MUST be square and as large as the space left after the players strip and the tray allow, and nothing MUST be drawn over any board square. Every premium label and tile letter MUST fit inside its square at every supported size. The page MUST lay out for a phone in portrait (players strip above the board, rack and action bar below it) and for a wide desktop window (players and faces to the left of the board, rack and action bar under the board, last plays and the bag to the right).

#### Scenario: Opening a game

- **WHEN** a Letter Tiles game starts and seat A holds 7 tiles
- **THEN** seat A's page shows 225 squares with the centre star and seat A's 7 tiles

#### Scenario: A phone

- **WHEN** the page is 390 by 844 pixels
- **THEN** all 225 squares, the players strip, the 7 rack tiles, and the Submit button are fully visible without scrolling, and no two of them overlap

#### Scenario: A desktop

- **WHEN** the page is 1440 by 900 pixels
- **THEN** the players are to the left of the board, the last plays are to the right, the rack and Submit are under the board, all are fully visible without scrolling, and none overlap

### Requirement: Placing tiles is local until Submit

Dragging a rack tile onto an empty square, or tapping a rack tile and then an empty square, MUST place that tile there on the page only. Dragging an unsent tile to another empty square MUST move it, and dragging it back to the rack, or tapping it, MUST return it to the rack. Dropping a tile anywhere else MUST return it to where it came from. Recall MUST return all unsent tiles. Shuffle MUST reorder the rack tiles on this page only and MUST NOT send anything. Placing a blank MUST ask which letter it stands for. No play is sent to the server until Submit.

#### Scenario: Place and take back

- **WHEN** seat A taps rack tile `C`, then square 111, then taps the `C` on square 111
- **THEN** `C` is back on the rack and no play was sent

#### Scenario: Drag and move

- **WHEN** seat A drags rack tile `C` onto square 111, then drags it from square 111 to square 126
- **THEN** square 126 shows an unsent `C`, square 111 is empty, and no play was sent

#### Scenario: Shuffle

- **WHEN** seat A presses Shuffle
- **THEN** seat A's rack shows the same tiles, and nothing is sent to the server

#### Scenario: A blank

- **WHEN** seat A places a blank on square 112 and chooses `A`
- **THEN** square 112 shows `A` with no point value, and Submit sends it with `blank: true`

### Requirement: The score panel shows every player

The page MUST show a badge for each playing seat with its score and tile count, and MUST ring the seat to move. The player's own badge and their call partner's badge MUST hold the live faces when the call is up; other seats show their seat letter. A status line MUST say "Your turn" on the player's turn, "B is thinking" (with the seat to move) otherwise, and the ending when the game is over. The page MUST show the tiles left in the bag and the last play's words and score.

#### Scenario: After the first play

- **WHEN** seat A has played `CAT` for 10
- **THEN** the badges show A 10 and B 0, seat B is ringed, the bag shows 83 in a two-player game, the last play reads "A played CAT for 10", and seat B's status line reads "Your turn"

#### Scenario: Faces on the badges

- **WHEN** seats A and B are in a Letter Tiles game with the call up
- **THEN** seat A's page shows seat A's camera in A's badge and seat B's camera in B's badge, and no separate face dock

## ADDED Requirements

### Requirement: A live score preview while placing

Whenever the unsent tiles change and at least one is on the board, the page MUST send `tiles-preview` with them. When the answer for the current unsent tiles arrives, the page MUST outline the unsent tiles and show the score in a bubble next to them, and the Submit button MUST read "Submit" followed by that score. When the answer is a refusal, the bubble MUST show the short reason instead (for `not-a-word`, the unknown words) and the unsent tiles MUST stay where they are. An answer for a placement the page no longer shows MUST be ignored. With no unsent tiles, no bubble is shown.

#### Scenario: Previewing CAT

- **WHEN** seat A places `C`, `A`, `T` on squares 111 to 113 and the answer is words `["CAT"]` score 10
- **THEN** the bubble shows 10 and the Submit button reads "Submit 10"

#### Scenario: A stale answer

- **WHEN** seat A places `C` and `A`, then places `T` before the answer for `C A` arrives
- **THEN** the answer for `C A` is ignored and the bubble waits for the answer for `C A T`

#### Scenario: An unknown word before submitting

- **WHEN** the answer for the unsent tiles is refusal `not-a-word` naming `QXZ`
- **THEN** the bubble reads "QXZ is not in the word list" and Submit is still allowed on the player's turn

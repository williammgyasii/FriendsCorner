## MODIFIED Requirements

### Requirement: The score panel shows every player

The page MUST show a scoreboard panel for each playing seat with its score and tile count, and MUST mark the seat to move. The player's own panel and their call partner's panel MUST hold the live faces when the call is up; other seats show their seat letter. On a phone the panels sit above the board; on a desktop they sit in a column beside the board, never over it. After a play, the panel of the seat that scored MUST show the gain ("+10"). A status line MUST guide the turn, in this order: the ending when the game is over; while placing, the preview's refusal or the previewed words and score ("CAT for 10"); on the player's turn, "Your turn", with "the first word must cover the star" added on an empty board; otherwise "B is thinking" (with the seat to move). The page MUST show the tiles left in the bag and the last play's words and score.

#### Scenario: After the first play

- **WHEN** seat A has played `CAT` for 10
- **THEN** the panels show A 10 and B 0, A's panel shows "+10", seat B is marked, the bag shows 83 in a two-player game, the last play reads "A played CAT for 10", and seat B's status line reads "Your turn"

#### Scenario: Faces on the badges

- **WHEN** seats A and B are in a Letter Tiles game with the call up
- **THEN** seat A's page shows seat A's camera in A's panel and seat B's camera in B's panel, and no separate face dock

#### Scenario: The first move

- **WHEN** the board is empty and it is seat A's turn
- **THEN** seat A's status line reads "Your turn · the first word must cover the star"

#### Scenario: Guiding while placing

- **WHEN** seat A has unsent tiles and the kept preview is `CAT` for 10, or is refused because the tiles do not touch the board
- **THEN** the status line reads "CAT for 10", or "New tiles must touch tiles on the board"

## ADDED Requirements

### Requirement: The stage can go full screen and moves with the game

The page MUST fill the window and give the board most of it: each face fills its player's panel with the seat, score, and tile count laid over it, and on a desktop the rack sits under the board while the actions sit beside it, so the board is limited only by the window height. Where the browser allows the page to go full screen, the page MUST show a full-screen button that enters and leaves full screen; where it does not, the button MUST NOT be shown. Tiles that land on the board and tiles that refill the rack SHOULD animate in, and scores SHOULD count up, unless the device asks for reduced motion.

#### Scenario: The board on a desktop

- **WHEN** the page is 1440 by 900
- **THEN** the board is at least 88% of the window height, each face fills its panel in the side column, and nothing overlaps

#### Scenario: The board on a phone

- **WHEN** the page is 390 by 844
- **THEN** the board is at least 97% of the window width and starts within 140 pixels of the top

#### Scenario: A desktop browser

- **WHEN** the browser allows full screen and seat A presses "Full screen"
- **THEN** the page asks the browser for full screen, and the button then reads "Leave full screen"

#### Scenario: An iPhone

- **WHEN** the browser does not allow the page to go full screen
- **THEN** there is no full-screen button and the page still fills the window

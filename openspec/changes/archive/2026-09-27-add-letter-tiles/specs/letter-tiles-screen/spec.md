## Purpose

What a player sees and can do on the page during a Letter Tiles game, without ever showing another player's tiles.

## ADDED Requirements

### Requirement: The board and the rack are drawn

The page MUST draw the 15 by 15 board with each premium square labelled (`DL`, `TL`, `DW`, `TW`, and a star on the centre), every tile on the board with its letter and point value (a blank shows its letter and no value), and the player's own rack with each tile's letter and value.

#### Scenario: Opening a game

- **WHEN** a Letter Tiles game starts and seat A holds 7 tiles
- **THEN** seat A's page shows 225 squares with the centre star and seat A's 7 tiles

### Requirement: Placing tiles is local until Submit

Tapping a rack tile and then an empty square MUST place that tile there on the page only. Tapping a placed-but-unsent tile MUST return it to the rack. Recall MUST return all unsent tiles. Placing a blank MUST ask which letter it stands for. Nothing is sent to the server until Submit.

#### Scenario: Place and take back

- **WHEN** seat A taps rack tile `C`, then square 111, then taps the `C` on square 111
- **THEN** `C` is back on the rack and the server was sent nothing

#### Scenario: A blank

- **WHEN** seat A places a blank on square 112 and chooses `A`
- **THEN** square 112 shows `A` with no point value, and Submit sends it with `blank: true`

### Requirement: Submit, Exchange, and Pass follow the turn

Submit MUST be enabled only on the player's turn with at least one unsent tile, and MUST send those tiles as one play. Exchange MUST be enabled only on the player's turn while the bag holds at least 7 tiles, and MUST let the player choose which rack tiles to send back. Pass MUST be enabled only on the player's turn. A seat that is not to move MUST still be able to arrange unsent tiles.

#### Scenario: Not your turn

- **WHEN** seat B's page shows seat A to move
- **THEN** Submit, Exchange, and Pass are disabled on seat B's page

#### Scenario: Small bag

- **WHEN** it is seat A's turn and the bag holds 5
- **THEN** Exchange is disabled and Pass is enabled

### Requirement: The score panel shows every player

The page MUST show each playing seat's score and tile count, highlight the seat to move, show the tiles left in the bag, and show the last play's words and score.

#### Scenario: After the first play

- **WHEN** seat A has played `CAT` for 10
- **THEN** the panel shows A 10, B 0, seat B highlighted, bag 83 in a two-player game, and "A played CAT for 10"

### Requirement: Refusals and the end are clear

When the player's message has a refusal, the page MUST show a short reason (for `not-a-word`, the unknown words) and MUST keep the unsent tiles where they were. When the game ends, the page MUST show the winners and final scores and a Rematch button.

#### Scenario: A refused word

- **WHEN** seat A submits `QXZ` and the refusal is `not-a-word`
- **THEN** the page shows "QXZ is not in the word list" and `Q`, `X`, `Z` stay on their squares, unsent

#### Scenario: Game over

- **WHEN** the game ends with seat B winning 212 to 180
- **THEN** both pages show "B wins, 212 to 180" and a Rematch button

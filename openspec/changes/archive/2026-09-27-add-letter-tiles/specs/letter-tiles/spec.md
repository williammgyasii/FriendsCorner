## Purpose

The rules of Letter Tiles, a crossword tile game for 2 to 4 players: the bag, racks, where tiles may go, which words a play forms, how they score, whose turn it is, and how the game ends.

## ADDED Requirements

### Requirement: The board has fixed premium squares

The board MUST be 15 by 15. Squares are numbered 0 to 224, row by row, so square = row x 15 + column. The centre square is 112 (row 7, column 7). The board MUST have 8 triple-word squares, 17 double-word squares including the centre, 12 triple-letter squares, and 24 double-letter squares, and the layout MUST look the same when rotated 90 degrees or mirrored.

#### Scenario: Landmark squares

- **WHEN** the layout is read
- **THEN** squares 0, 7, 14, 105, 119, 210, 217, and 224 are triple word; square 112 is double word; squares 20 and 24 are triple letter; squares 3 and 11 are double letter

#### Scenario: Symmetry

- **WHEN** the layout is rotated 90 degrees or mirrored left to right
- **THEN** every square keeps the same premium

### Requirement: The bag holds 100 tiles

A new game MUST fill the bag with exactly these tiles: A 9, B 2, C 2, D 4, E 12, F 2, G 3, H 2, I 9, J 1, K 1, L 4, M 2, N 6, O 8, P 2, Q 1, R 6, S 4, T 6, U 4, V 2, W 2, X 1, Y 2, Z 1, and 2 blanks. Letter values MUST be: A, E, I, L, N, O, R, S, T, U = 1; D, G = 2; B, C, M, P = 3; F, H, V, W, Y = 4; K = 5; J, X = 8; Q, Z = 10; blank = 0. The bag order MUST come from a shuffle whose randomness can be fixed for tests.

#### Scenario: A fresh bag

- **WHEN** a new game fills the bag
- **THEN** it holds 100 tiles with 12 E, 2 blanks, and a total value of 187 points

#### Scenario: A fixed shuffle repeats

- **WHEN** two games are started with the same fixed randomness
- **THEN** every player draws the same tiles in both games

### Requirement: Players start with seven tiles and take turns in seat order

Every playing seat MUST draw 7 tiles at the start. The first seat in playing order MUST move first, and turns MUST pass to the next playing seat in order, wrapping around. A command from a seat that is not to move MUST be refused.

#### Scenario: A three-player start

- **WHEN** seats A, B, and C start a game
- **THEN** each holds 7 tiles, the bag holds 79, and seat A is to move

#### Scenario: Out of turn

- **WHEN** seat B tries to play while seat A is to move
- **THEN** the play is refused and nothing changes

### Requirement: A play is a legal line of tiles

A play MUST place 1 to 7 tiles from the player's rack on empty squares, all in one row or all in one column. The tiles MUST form one unbroken line together with any tiles already on the board between them. The first play of a game MUST cover square 112 and place at least 2 tiles. Every later play MUST place at least one tile next to (up, down, left, or right) a tile already on the board. A play that breaks any of these rules MUST be refused, and the player keeps the tiles and the turn.

#### Scenario: First play misses the centre

- **WHEN** the first play places `C A T` on squares 0, 1, 2
- **THEN** it is refused because it does not cover square 112

#### Scenario: Tiles not in one line

- **WHEN** a play places tiles on squares 112 and 128
- **THEN** it is refused because they share neither a row nor a column

#### Scenario: A gap in the line

- **WHEN** the first play places tiles on squares 111 and 113 with 112 empty
- **THEN** it is refused because the line is broken

#### Scenario: A floating play

- **WHEN** a later play places tiles that touch no tile already on the board
- **THEN** it is refused

#### Scenario: A tile the player does not hold

- **WHEN** a player plays a `Q` that is not on their rack
- **THEN** it is refused

### Requirement: A play forms words that must all be real

The words a play forms are the main word (the full line of tiles through the placed tiles, in the direction they were placed) and every cross-word (the full line through each placed tile in the other direction) that is 2 or more letters long. A single placed tile forms a word in each direction that reaches 2 or more letters. Every formed word MUST be in the ENABLE word list, ignoring case. If any formed word is not, the play MUST be refused and the refusal MUST name the unknown words.

#### Scenario: A real word

- **WHEN** the first play places `C A T` across squares 111, 112, 113
- **THEN** the play is accepted and forms one word, `CAT`

#### Scenario: An unknown word

- **WHEN** the first play places `Q X Z` across squares 111, 112, 113
- **THEN** it is refused, the refusal names `QXZ`, and the player keeps the tiles and the turn

#### Scenario: A cross-word is also checked

- **WHEN** `CAT` is on squares 111 to 113 and a play places `X` on square 128, under the `T`
- **THEN** it is refused and the refusal names `TX`

#### Scenario: Hooking a word

- **WHEN** `CAT` is on squares 111 to 113 and a play places `S` on square 114
- **THEN** the play forms `CATS` and is accepted

### Requirement: Scoring counts letters, premiums once, and a bonus

Each formed word MUST score the sum of its letter values, where a newly placed tile on a double-letter or triple-letter square counts 2 or 3 times its value, and then the word total is multiplied by 2 or 3 for each newly placed tile on a double-word or triple-word square. Premium squares under tiles placed in earlier turns MUST NOT count again. A blank MUST count 0 wherever it lies. The play scores the sum of all formed words, plus 50 when all 7 tiles of a full rack are placed.

#### Scenario: First word on the centre

- **WHEN** the first play places `C A T` across squares 111, 112, 113
- **THEN** it scores 10: (3 + 1 + 1) x 2 for the centre double word

#### Scenario: Premium used only once

- **WHEN** `CAT` is on squares 111 to 113 and a later play adds `S` on square 114 to make `CATS`
- **THEN** the play scores 6, and the centre double word does not count again

#### Scenario: A blank scores zero

- **WHEN** the first play places `C`, a blank shown as `A`, and `T` across squares 111, 112, 113
- **THEN** it scores 8: (3 + 0 + 1) x 2

#### Scenario: All seven tiles

- **WHEN** a player places all 7 rack tiles in one accepted play
- **THEN** 50 points are added to the words' total

### Requirement: The rack refills after a play

After an accepted play, the player MUST draw from the bag until they hold 7 tiles or the bag is empty, and the turn MUST pass to the next playing seat.

#### Scenario: Refill

- **WHEN** a player with 7 tiles plays 3 and the bag holds 50
- **THEN** the player holds 7 tiles and the bag holds 47

### Requirement: Exchange and pass

On their turn, a player MAY exchange 1 to 7 tiles from their rack instead of playing, only while the bag holds at least 7 tiles. The exchanged tiles MUST go back into the bag, the bag MUST be shuffled, and the player MUST draw the same number. A player MAY pass instead. Both score 0 and pass the turn.

#### Scenario: Exchange with a full bag

- **WHEN** seat A exchanges 3 tiles while the bag holds 20
- **THEN** seat A still holds 7 tiles, the bag still holds 20, seat A's score is unchanged, and seat B is to move

#### Scenario: Exchange with a small bag

- **WHEN** seat A tries to exchange while the bag holds 6
- **THEN** it is refused and seat A keeps the turn

#### Scenario: Pass

- **WHEN** seat A passes
- **THEN** seat A's score is unchanged and seat B is to move

### Requirement: The game ends and racks are settled

The game MUST end when a player places their last tile while the bag is empty. That player MUST gain the total value of every other player's rack, and every other player MUST lose their own rack's value. The game MUST also end after 6 scoreless turns in a row (passes, exchanges, or plays that score 0; refused commands are not turns), and then every player MUST lose their own rack's value. The winners are the players with the highest final score; a tie means several winners. After the end, every play, exchange, or pass MUST be refused.

#### Scenario: Going out

- **WHEN** the bag is empty, seat A plays their last tile, and seat B holds `Q` and `E`
- **THEN** the game ends, seat A gains 11, and seat B loses 11

#### Scenario: Six scoreless turns

- **WHEN** two players pass 3 times each in a row
- **THEN** the game ends and each player loses the value of their own rack

#### Scenario: A tie

- **WHEN** the game ends with seats A and B both on 210 and seat C on 150
- **THEN** seats A and B are both winners

### Requirement: Rematch after the end

A rematch MUST be refused while a game is in progress. After the end, a rematch MUST start a fresh game with a new bag, empty board, 0 scores, and the first move going to the seat after the one that moved first in the previous game.

#### Scenario: Rematch rotates the starter

- **WHEN** seats A and B finish a game that seat A started, and a rematch is asked for
- **THEN** a new game starts and seat B moves first

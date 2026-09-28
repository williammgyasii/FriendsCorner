## ADDED Requirements

### Requirement: A placement can be previewed without playing it

A player MUST be able to ask what a placement would score without playing it. A preview MUST apply the same line, rack, centre, connection, and word rules as a play, and MUST answer either the formed words with the score the play would get, or the refusal the play would get. A preview MUST NOT change the board, any rack, the bag, any score, the turn, the scoreless-turn count, or any stored refusal. A preview MUST NOT depend on whose turn it is, so a player can plan while waiting. After the game has ended, a preview MUST answer `game-over`.

#### Scenario: Previewing a real word

- **WHEN** seat B holds `C`, `A`, `T` and, while seat A is to move, previews `C A T` across squares 111, 112, 113 as the first play
- **THEN** the answer is words `["CAT"]` and score 10, and the board, racks, bag, scores, and turn are unchanged

#### Scenario: Previewing an unknown word

- **WHEN** a player previews `Q X Z` across squares 111, 112, 113 as the first play
- **THEN** the answer is refusal `not-a-word` naming `QXZ`, and that player has no stored refusal afterwards

#### Scenario: Previewing a broken line

- **WHEN** a player previews tiles on squares 111 and 113 with 112 empty on the first play
- **THEN** the answer is refusal `gap`

#### Scenario: A preview matches the play

- **WHEN** a player previews a placement, then plays the same placement on their turn
- **THEN** the play's words and score equal the preview's

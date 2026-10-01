## ADDED Requirements

### Requirement: The app is dark

Every screen (the lobby, the room, tic-tac-toe, chess, Letter Tiles, Murder Mystery) MUST use a dark background with light text, from one set of colour tokens. The seat colours MUST stay the same.

#### Scenario: The lobby is dark

- **WHEN** the lobby is shown
- **THEN** its background token is a dark colour and its text is light

### Requirement: The lobby's games are cards

Each game in the lobby MUST look like a playing card: portrait, with a small corner radius, a framed face, and the game's glyph as corner pips. The picked card MUST lift and glow.

#### Scenario: A card per game

- **WHEN** the lobby is shown
- **THEN** each game is a portrait card with its name, its player count, and two corner pips

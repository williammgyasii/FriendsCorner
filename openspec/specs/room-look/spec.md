# room-look Specification

## Purpose
Make the shared floor look like a small room and each seat look like a person, so two people can tell themselves apart before the cameras connect.

## Requirements

### Requirement: The floor reads as a room

The page MUST draw the 480 by 320 floor as a room. The top 64 pixels MUST be the wall. The remaining 256 pixels MUST be the floor. The wall and the floor MUST be different colors.

#### Scenario: A seat stands on the floor

- **WHEN** a seat is at `(240, 160)`
- **THEN** the character is drawn in the floor band, below the 64-pixel wall

### Requirement: Each seat is a character

Each occupied seat MUST be drawn as a figure with a head and a body. The two seats MUST use different body colors. A figure MUST stay fully inside the 480 by 320 floor, including when the seat position is on the edge.

#### Scenario: One person is in the room

- **WHEN** only seat A is occupied
- **THEN** the page shows one figure and no second figure

#### Scenario: Both people stand on the same spot

- **WHEN** seat A and seat B are both at `(240, 160)`
- **THEN** both figures are visible and their drawn centers are at least 24 pixels apart

### Requirement: Movement still owns the position

The character MUST be drawn at the position the server last sent. The page MUST NOT invent a different position for the engine.

#### Scenario: A direction moves the character

- **WHEN** the server reports seat A at `(400, 160)`
- **THEN** seat A's figure is drawn at x `400` on the floor, shifted only when the two figures would overlap or would leave the floor

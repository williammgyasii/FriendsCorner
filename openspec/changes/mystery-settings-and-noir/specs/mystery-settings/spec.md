## ADDED Requirements

### Requirement: The game master chooses a mystery level and mode

The lobby MUST hold mystery settings: a level (`easy` or `hard`) and a mode (`together` or `race`), defaulting to easy and together. Only the host MAY change them, and only before the countdown starts. Changing them MUST clear every guest's ready. Every seat MUST be sent the current settings.

#### Scenario: The host sets hard race

- **WHEN** the host sets level hard and mode race
- **THEN** the lobby holds hard race and every seat is told so

#### Scenario: A guest cannot change settings

- **WHEN** a guest sets level hard
- **THEN** the settings stay easy together

#### Scenario: Settings are locked during the countdown

- **WHEN** the countdown has started and the host sets mode race
- **THEN** the settings do not change

#### Scenario: Changing settings clears ready

- **WHEN** a guest is ready and the host changes the level
- **THEN** that guest is no longer ready

### Requirement: The settings start the game

When the countdown ends on Murder Mystery, the game MUST start with the lobby's settings, and a rematch MUST keep them.

#### Scenario: A hard race starts as a hard race

- **WHEN** the host picked Murder Mystery with hard race and the countdown ends
- **THEN** the game's settings are hard race

### Requirement: The lobby shows the settings when Murder Mystery is picked

When the pick is Murder Mystery, the lobby MUST show a settings panel with the two levels and the two modes, each with a one-line explanation. The host MUST be able to choose; a guest MUST see the choice but not change it.

#### Scenario: The host picks hard

- **WHEN** the host taps Hard in the panel
- **THEN** the page sends `{"type":"mystery-settings","level":"hard","mode":"together"}`

#### Scenario: A guest sees the choice

- **WHEN** a guest's lobby says hard race
- **THEN** the panel marks Hard and Race as chosen and its choices are disabled

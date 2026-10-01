## ADDED Requirements

### Requirement: The room's tick drives game clocks

Every room tick MUST let the running game advance its clocks, and a tick that changes the game MUST be saved and broadcast like a move.

#### Scenario: A final answer locks on a tick

- **WHEN** a together game is accusing and the room ticks past the lock
- **THEN** the game is revealed, saved, and broadcast

### Requirement: The writer is told the level

The case request MUST carry the game's level, and the writer MUST send it to the model with the theme. The model's reply MUST include `minutes` (5–20) and `mood`, and the schema MUST require them.

#### Scenario: A hard request

- **WHEN** a hard game asks for a case
- **THEN** the request to the model's input says `Difficulty: hard`

### Requirement: The message carries settings, clocks, and per-seat secrets

The lobby section MUST carry `mystery: {level, mode}`. The mystery section MUST be built for each seat. It MUST carry:

- `level`, `mode` and `mood`
- `endsInMs` and `locksInMs`, each null when there is no clock
- each lead's `by` (the seat that opened it, when you may see it)
- `out` (the seats that are out)
- in the outcome, `winner` and `timedOut`, with `accused` null when nobody accused

The client MAY send `{"type":"mystery-settings","level","mode"}` and `{"type":"mystery-withdraw"}`.

#### Scenario: The partner's race leads stay secret

- **WHEN** A has opened `c1` in a race
- **THEN** B's message has `c1` with a null text and a null `by`

### Requirement: Saved games keep the new fields

The saved row MUST keep the settings, who opened each lead, and both clocks. A row that can't be read MUST load as nothing.

#### Scenario: A hard race round-trips

- **WHEN** a hard race with A's `c1` opened and a clock is saved and loaded
- **THEN** the loaded state equals the saved one

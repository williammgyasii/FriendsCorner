## Purpose

Every game plugs into a room the same way, so adding a game does not change how the existing games behave or what the page receives for them.

## ADDED Requirements

### Requirement: Existing games behave exactly as before

Tic-tac-toe and chess MUST keep their current rules, turn order, rematch behavior, save, and restore. The state message each seat receives for a tic-tac-toe or chess room MUST be identical to the message sent before this change for the same room, except for one added field, `tiles`, which is `null` in those rooms.

#### Scenario: A chess move after the change

- **WHEN** seat A plays `e2` to `e4` in a new chess room
- **THEN** the state message both seats receive equals the message the room sent for the same move before this change, plus `"tiles":null`

#### Scenario: A saved tic-tac-toe game after the change

- **WHEN** a tic-tac-toe room with X on square 4 is restored after a server restart
- **THEN** the board shows X on square 4 and seat B is to move, as before

### Requirement: A room runs one game at a time

Once a room has launched a game, it MUST refuse to launch a different game. Commands meant for a game that is not running MUST be refused and MUST NOT change the room.

#### Scenario: A chess move in a tic-tac-toe room

- **WHEN** a room is running tic-tac-toe and seat A sends a chess move
- **THEN** the room is unchanged and nothing is saved

#### Scenario: Launching a second game

- **WHEN** a room is running chess and the countdown for tic-tac-toe finishes
- **THEN** the room keeps running chess

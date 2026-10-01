## MODIFIED Requirements

### Requirement: Existing games behave exactly as before

Tic-tac-toe and chess MUST keep their current rules, turn order, rematch behavior, save, and restore. The state message each seat receives for a tic-tac-toe or chess room MUST be identical to the message sent before this change for the same room, except for two added fields, `tiles` and `mystery`, which are `null` in those rooms. A Letter Tiles room's message MUST likewise gain only `"mystery": null`.

#### Scenario: A chess move after the change

- **WHEN** seat A plays `e2` to `e4` in a new chess room
- **THEN** the state message both seats receive equals the message the room sent for the same move before this change, plus `"tiles":null` and `"mystery":null`

#### Scenario: A saved tic-tac-toe game after the change

- **WHEN** a tic-tac-toe room with X on square 4 is restored after a server restart
- **THEN** the board shows X on square 4 and seat B is to move, as before

## ADDED Requirements

### Requirement: A preview is answered to the asking seat only

The room MUST accept `tiles-preview` from a playing seat, with `tiles` in the same shape as `tiles-play`. The room MUST answer with one message to that seat only: `{"type":"tiles-preview","tiles":[...the asked squares...],"words":[...],"score":N}` for a legal placement, or `{"type":"tiles-preview","tiles":[...],"refusal":{"reason":"...","words":[...]}}` otherwise. The answer MUST echo the asked squares so the page can drop an answer for a placement it has since changed. A preview MUST NOT be saved, MUST NOT cause a state message to any seat, and MUST NOT be answered for a seat that is not playing or when no Letter Tiles game is running. A malformed `tiles-preview` MUST be ignored.

#### Scenario: Seat A previews CAT

- **WHEN** seat A holds `C`, `A`, `T` and sends `{"type":"tiles-preview","tiles":[{"square":111,"letter":"C"},{"square":112,"letter":"A"},{"square":113,"letter":"T"}]}` as the first play
- **THEN** seat A receives a `tiles-preview` message with words `["CAT"]` and score 10, seat B receives nothing, and nothing is recorded for saving

#### Scenario: A preview while tic-tac-toe is running

- **WHEN** the room is running tic-tac-toe and a seat sends `tiles-preview`
- **THEN** no message is sent to any seat

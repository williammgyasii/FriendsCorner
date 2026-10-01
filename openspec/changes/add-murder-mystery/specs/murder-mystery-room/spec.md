## Purpose

How a murder mystery runs inside a room: picking it in the lobby, waiting while a case is written, what each state message may reveal, the commands the page sends, and surviving a server restart.

## ADDED Requirements

### Requirement: Murder mystery is a lobby game for two

The lobby MUST offer a game with id `mystery` for exactly 2 players. When its countdown finishes, the room's world MUST be `mystery`, the game MUST be in the `writing` phase, and seats A and B MUST be its players.

#### Scenario: Starting a mystery

- **WHEN** the host of a two-seat room picks `mystery`, the guest is ready, and the countdown finishes
- **THEN** the room's world is `mystery` and every seat's next message shows phase `writing`

### Requirement: The server writes the case while players wait

When a mystery game enters `writing`, the server MUST ask the case writer for one case, without blocking the room: players MUST keep receiving state messages while the case is written. When the writer returns nothing (an error), or a case that is not well-formed or not fair, the server MUST ask once more. When a fair case arrives, the game MUST move to `investigating` and every seat MUST receive a state message. When the second attempt also fails, or any attempt takes longer than 60 seconds, the game MUST move to `failed` and every seat MUST receive a state message. Only one case MUST be written for a room at a time.

#### Scenario: A fair case on the first try

- **WHEN** the game is `writing` and the writer returns a fair case
- **THEN** both seats receive a message with phase `investigating`, 8 leads left, and the case's 4 suspects

#### Scenario: An unfair case, then a fair one

- **WHEN** the writer first returns a case where a proof clears the killer, then a fair case
- **THEN** the writer was asked twice and the game is `investigating` with the second case

#### Scenario: Two unfair cases

- **WHEN** the writer returns two unfair cases in a row
- **THEN** the writer was asked exactly twice and the game is `failed`

#### Scenario: The writer is too slow

- **WHEN** the writer has not answered after 60 seconds
- **THEN** the game is `failed`

### Requirement: A state message never gives the case away

Every state message for a mystery room MUST include a `mystery` section with: the `phase` (`writing`, `investigating`, `revealed`, or `failed`); once a case exists, its `title`, `setting`, `victim`, `suspects` (id, name, role, motive, alibi), and `places` (id, name); the `leads` (id, `kind` `clue` or `statement`, the place or suspect it is `about`, and its `text` only when open, otherwise `null`); `leadsLeft`; the `picks` of seats A and B (a suspect id or `null`); and the `outcome` (killer, how, why, story, `right`, `score`) only when revealed, otherwise `null`. Before the reveal, a message MUST NOT contain the killer, the solution, the text of a closed lead, the proofs, or which statement is a lie.

#### Scenario: Mid-investigation

- **WHEN** the killer is `s3`, `c1` and `t4` are open, and 6 leads remain
- **THEN** both seats' messages show the text of `c1` and `t4`, `null` text for the other 12 leads, `leadsLeft` 6, `outcome` `null`, and nowhere contain `s3` as the killer, the reveal story, or any proof

#### Scenario: After the reveal

- **WHEN** both seats picked `s3` and the killer is `s3`
- **THEN** both seats' messages have `outcome` with killer `s3`, `right` true, and the reveal story

### Requirement: Commands from the page

The room MUST accept these messages from a playing seat: `mystery-open` with a `lead` id, `mystery-accuse` with a `suspect` id, and `mystery-rematch`. A malformed message, or one from a seat that is not playing, MUST be ignored and MUST NOT change the room.

#### Scenario: Opening a lead

- **WHEN** seat A sends `{"type":"mystery-open","lead":"c1"}` during an investigation
- **THEN** both seats' next messages show the text of `c1` and 7 leads left

#### Scenario: A malformed accusation

- **WHEN** a `mystery-accuse` message has no `suspect`
- **THEN** it is ignored and the room is unchanged

### Requirement: The model key never reaches the page

The OpenAI key MUST be read by the server from its configuration only (user secrets in development, a deployment secret in production). It MUST NOT appear in the repository, in any state message, or in anything served to the page. With no key configured, starting a mystery MUST end in `failed`, and the other games MUST work as before.

#### Scenario: No key configured

- **WHEN** the server has no OpenAI key and a mystery countdown finishes
- **THEN** the game is `failed` and tic-tac-toe, chess, and Letter Tiles still start normally in other rooms

### Requirement: A mystery survives a restart

After every accepted command, and whenever the phase changes, the room MUST save the whole game: the case, which leads are open, leads left, both picks, the phase, and the outcome. When a saved room link is opened after a server restart, the room MUST reopen the same game. A game saved while `writing` MUST reopen as `failed`, so the players can rematch.

#### Scenario: Restart mid-investigation

- **WHEN** `c1` and `t4` are open, seat A has picked `s2`, the server restarts, and both players reopen the room link
- **THEN** the same case is shown with `c1` and `t4` open, 6 leads left, and seat A's pick `s2`

#### Scenario: Restart while writing

- **WHEN** the server restarts while a case is being written
- **THEN** the reopened game is `failed`

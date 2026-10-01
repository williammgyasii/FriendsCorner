## ADDED Requirements

### Requirement: The level sets the leads, the clock, and the bonus

An easy game MUST have 8 leads and no clock. A hard game MUST have 6 leads and a clock of the case's `minutes`, clamped to 5–20, that starts at the first tick after the case arrives. Solving a hard case MUST add 50 to the score.

#### Scenario: Hard has six leads

- **WHEN** a hard game receives its case and 6 leads are opened
- **THEN** a seventh is refused

#### Scenario: The clock starts on the first tick

- **WHEN** a hard case with `minutes` 12 arrives and the room ticks at 10:00
- **THEN** the game ends at 10:12

#### Scenario: Out of time

- **WHEN** a hard together game ticks past its end
- **THEN** it is revealed as timed out, with no accusation and a score of 0

#### Scenario: Hard bonus

- **WHEN** a hard together game is solved with 2 leads left
- **THEN** the score is 100 + 20 + 50 = 170

### Requirement: Together ends with a final answer

In together mode, when both players pick the same suspect, the game MUST enter `accusing` instead of revealing. The first tick after that MUST set a lock 5 seconds later, and a tick at or past the lock MUST reveal. While accusing, either player MAY switch their pick or withdraw it, which MUST return the game to `investigating` and clear the lock. No lead may be opened while accusing. This replaces "the same pick locks and reveals" from `add-murder-mystery`.

#### Scenario: Matching picks start the final answer

- **WHEN** both pick `s3`
- **THEN** the phase is accusing, and after a tick at 10:00 it locks at 10:00:05

#### Scenario: The lock reveals

- **WHEN** the game is accusing with a lock at 10:00:05 and ticks at 10:00:05
- **THEN** it is revealed

#### Scenario: Holding on cancels

- **WHEN** the game is accusing and player B withdraws
- **THEN** the phase is investigating, B has no pick, and there is no lock

### Requirement: Race is first to the killer

In race mode, each player MUST have their own lead budget and see the text of only the leads they opened. An accusation MUST be final. A right accusation MUST reveal with that player as the winner, scoring 100 + 10 per lead they have left (+50 on hard). A wrong one MUST put that player out: they can no longer open or accuse. When every player is out, or the clock runs out, the game MUST reveal with no winner. Until the reveal, a player MUST NOT see their partner's pick or leads, but MUST see whether their partner is out.

#### Scenario: Private leads

- **WHEN** A opens `c1` in a race
- **THEN** A has 7 leads left, B still has 8, and B's view has no text for `c1`

#### Scenario: A right accusation wins

- **WHEN** B accuses `s3`, the killer, with 5 leads left
- **THEN** the game is revealed with B as the winner and a score of 150

#### Scenario: A wrong accusation puts you out

- **WHEN** A accuses `s1`
- **THEN** A is out, A can't open a lead, B's view says A is out, and B's view doesn't say whom A accused

#### Scenario: Both out

- **WHEN** A and B both accuse wrongly
- **THEN** the game is revealed with no winner

### Requirement: The case carries minutes and a mood

A case MUST carry `minutes` (how long a good pair needs) and a `mood`, one of `frost`, `storm`, `velvet`, `garden`, `smoke`, or `gilded`. Every seat MUST see the mood from the moment the case arrives.

#### Scenario: The mood is shown

- **WHEN** a case with mood `frost` arrives
- **THEN** every seat's view has mood `frost`

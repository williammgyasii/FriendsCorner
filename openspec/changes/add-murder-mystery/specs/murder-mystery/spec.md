## Purpose

The rules of a co-operative murder mystery: what a case contains, when a written case is fair enough to play, and how two players spend leads, accuse together, and score.

## ADDED Requirements

### Requirement: A case has a fixed shape

A case MUST contain: a `title`, a `setting`, a `victim` (name and how they were found), exactly 4 suspects with ids `s1`–`s4` (each with a name, a role, a motive, and an alibi), exactly 3 places with ids `p1`–`p3` (each with a name), exactly 14 leads, hidden proofs, and a solution. The leads MUST be 6 clues with ids `c1`–`c6` (2 at each place) and 8 statements with ids `t1`–`t8` (2 by each suspect); every lead MUST have its text, and a statement MAY be marked as a lie. A proof MUST name one suspect and 1 to 3 distinct lead ids that, once all open, clear that suspect; there MUST be 1 to 12 proofs. The solution MUST name the killer, how, why, and a reveal story. Every name MUST be 1 to 40 characters and every other text 1 to 400 characters, except the reveal story, which MUST be 1 to 1200 characters. A clue MUST NOT be marked as a lie. A case that breaks any of these MUST be rejected.

#### Scenario: A well-formed case

- **WHEN** a case has 4 suspects, 3 places, 6 clues (2 per place), 8 statements (2 per suspect), and a solution naming `s3`
- **THEN** it is accepted as well-formed

#### Scenario: A missing statement

- **WHEN** a case has only 7 statements
- **THEN** it is rejected

#### Scenario: A clue too long

- **WHEN** clue `c2`'s text is 401 characters
- **THEN** the case is rejected

### Requirement: A case must be fair before it is played

A well-formed case MUST also pass all of these, or be rejected as unfair:

1. The killer is one of `s1`–`s4`.
2. No proof clears the killer.
3. Every innocent suspect has at least one proof.
4. Some set of at most 6 leads contains a whole proof for each of the three innocents.
5. Exactly one of the killer's statements is marked as a lie, and no innocent's statement is.

#### Scenario: A fair case

- **WHEN** the killer is `s3`, `c1` and `t2` prove `s1`, `t3` proves `s2`, `c5` and `t8` prove `s4`, and no proof names `s3`
- **THEN** the case is fair

#### Scenario: A proof clears the killer

- **WHEN** the killer is `s3` and a proof of `c4` names `s3`
- **THEN** the case is rejected as unfair

#### Scenario: An innocent nobody can clear

- **WHEN** the killer is `s3` and no proof names `s2`
- **THEN** the case is rejected as unfair

#### Scenario: Too many leads needed

- **WHEN** each innocent's only proof needs 3 leads and no two proofs share a lead, so clearing all three needs 9 leads
- **THEN** the case is rejected as unfair

### Requirement: Two players investigate together with 8 leads

A mystery game MUST be for exactly 2 players. It MUST start with 8 leads to spend and all 14 leads closed. Either player MAY open any closed lead while leads remain; opening one MUST spend one lead and reveal its text to both players. Opening a lead that is already open, an unknown lead, or any lead when none remain MUST be refused and change nothing. The proofs and lie marks MUST never be shown to players before the reveal.

#### Scenario: Opening a clue

- **WHEN** seat A opens clue `c1` at the start
- **THEN** `c1` is open for both seats and 7 leads remain

#### Scenario: Opening it again

- **WHEN** `c1` is already open and seat B opens `c1`
- **THEN** it is refused and 7 leads remain

#### Scenario: No leads left

- **WHEN** 8 leads have been opened and seat A opens `t5`
- **THEN** it is refused and `t5` stays closed

### Requirement: The accusation locks only when both agree

Each player MUST be able to pick one suspect at any time before the reveal, including with leads left, and MAY change the pick. When both players' picks name the same suspect, the accusation MUST lock and the game MUST be revealed. Picking an unknown suspect MUST be refused.

#### Scenario: Picks differ

- **WHEN** seat A picks `s2` and seat B picks `s3`
- **THEN** the game is not revealed and each seat's pick is shown

#### Scenario: Both agree

- **WHEN** seat A picks `s3`, then seat B picks `s3`
- **THEN** the accusation locks on `s3` and the game is revealed

#### Scenario: Changing your mind

- **WHEN** seat A picked `s2`, seat B picked `s3`, and seat A changes to `s3`
- **THEN** the accusation locks on `s3`

### Requirement: The reveal shows the truth and the score

When revealed, the game MUST show the killer, how, why, the reveal story, and whether the accusation was right. The score MUST be 100 plus 10 for each unspent lead when right, and 0 when wrong. After the reveal, opening leads and picking MUST be refused.

#### Scenario: Right with 3 leads left

- **WHEN** the killer is `s3`, 5 leads were opened, and both pick `s3`
- **THEN** the reveal says right and the score is 130

#### Scenario: Wrong

- **WHEN** the killer is `s3` and both pick `s1`
- **THEN** the reveal says wrong, names `s3` as the killer, and the score is 0

### Requirement: Rematch writes a new case

After the reveal, or after a case could not be written, a rematch MUST start writing a new case for the same two players. A rematch MUST be refused while a case is being written or during an investigation.

#### Scenario: Rematch after the reveal

- **WHEN** the game is revealed and seat B asks for a rematch
- **THEN** the game goes back to writing a new case, with 8 leads and no picks

#### Scenario: Rematch mid-investigation

- **WHEN** 3 leads have been opened and seat A asks for a rematch
- **THEN** it is refused and the investigation continues

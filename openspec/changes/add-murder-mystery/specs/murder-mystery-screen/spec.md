## Purpose

What two players see and can do on the page during a murder mystery: waiting for the case, reading it, opening leads, agreeing on a suspect, and the reveal.

## ADDED Requirements

### Requirement: Waiting for the case

While the phase is `writing`, the page MUST show "Writing your case…" with a moving indicator (still under reduced motion). While `failed`, it MUST show "Couldn't write a case" and a Try again button that sends `mystery-rematch`.

#### Scenario: Writing

- **WHEN** a seat's message has phase `writing`
- **THEN** the page shows "Writing your case…" and no suspects or leads

#### Scenario: Failed

- **WHEN** a seat's message has phase `failed` and the player taps Try again
- **THEN** the page sends `{"type":"mystery-rematch"}`

### Requirement: The case board

During an investigation, the page MUST show the title, setting, and victim; the 4 suspects, each with name, role, motive, and alibi; the 3 places; and "N leads left". Each place MUST list its 2 clues and each suspect their 2 statements. A closed lead MUST be a button that sends `mystery-open` with its id, and MUST be disabled when no leads are left. An open lead MUST show its text. The page MUST lay out without horizontal scrolling at 390 by 844 and at 1440 by 900 pixels.

#### Scenario: Opening a clue from the page

- **WHEN** 8 leads are left and the player taps the closed clue `c1`
- **THEN** the page sends `{"type":"mystery-open","lead":"c1"}`

#### Scenario: Out of leads

- **WHEN** a message has `leadsLeft` 0
- **THEN** every closed lead's button is disabled

### Requirement: Both picks are visible

Each suspect MUST have an Accuse button that sends `mystery-accuse` with its id. The page MUST mark the suspect this player picked and, separately, the suspect the partner picked, and MUST say "Pick the same suspect to accuse" while the picks differ.

#### Scenario: Partner picked someone else

- **WHEN** this player's pick is `s2` and the partner's pick is `s3`
- **THEN** `s2` is marked "You", `s3` is marked with the partner's seat, and the page says "Pick the same suspect to accuse"

### Requirement: The reveal

When revealed, the page MUST say "Solved!" or "Not this time", name the killer, show how, why, the story, and the score, and offer a New case button that sends `mystery-rematch`.

#### Scenario: Solved

- **WHEN** a message has an outcome with `right` true, killer `s3`, and score 130
- **THEN** the page says "Solved!", names suspect `s3`, and shows 130

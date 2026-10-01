## ADDED Requirements

### Requirement: The screen looks like the case

The mystery screen MUST be a dark case file coloured by the case's mood (`data-mood`), with a moving ambience for that mood that stops under reduced motion. The case intro MUST be a folder with the title, setting, and victim. Suspects MUST be dossier cards; leads MUST be evidence cards that flip open when their text arrives. Nothing may scroll sideways at 390 px or 1440 px wide.

#### Scenario: The mood colours the screen

- **WHEN** the case's mood is `frost`
- **THEN** the screen has `data-mood="frost"`

### Requirement: The wait for a case reads as progress

While writing, the screen MUST show a line describing the writer's work, changing every few seconds.

#### Scenario: Writing lines

- **WHEN** the case is being written
- **THEN** the status says "Writing your case…" and a line such as "Choosing a victim…" is shown

### Requirement: The final answer is shown full screen

While accusing, the screen MUST show a full-screen overlay naming the accused suspect with the seconds until it locks, and a "Hold on" button that sends `mystery-withdraw`.

#### Scenario: The countdown

- **WHEN** the game is accusing `s3` (Clara Voss) with 3000 ms to the lock
- **THEN** the overlay says "Final answer: Clara Voss" and shows 3, and "Hold on" sends `{"type":"mystery-withdraw"}`

### Requirement: The clock is shown on hard

On a hard case the top bar MUST show the time left as m:ss, and warn when under a minute.

#### Scenario: Time left

- **WHEN** `endsInMs` is 125000
- **THEN** the clock reads 2:05

### Requirement: Race and outcomes read clearly

In a race, the hint MUST say "First to name the killer wins. You get one guess." A player who is out MUST see "You're out. Your partner is still looking." The reveal headline MUST be one of:

- "Solved!" or "Not this time" (together)
- "You win!", "Your partner wins", or "Nobody got it" (race)
- "Out of time" (the clock ran out)

#### Scenario: The partner wins a race

- **WHEN** a race is revealed with winner B and you are A
- **THEN** the headline is "Your partner wins"

#### Scenario: Out of time

- **WHEN** the outcome is timed out
- **THEN** the headline is "Out of time"

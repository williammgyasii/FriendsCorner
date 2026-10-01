## Purpose

Gives a signed-in person a home before a room exists, so a session and its shareable id start only when they ask.

## ADDED Requirements

### Requirement: A signed-in visit with no room shows the home

A signed-in visit to `/` with no `room` query and no pending checkout MUST show a home. The home MUST show the account's game name and a button labeled `Start a session`. When the account's plan is `corner`, `table`, or `house`, the home MUST also show `Corner`, `Table`, or `House`. When the account has no plan, the home MUST NOT show a plan name. The home MUST NOT show the text `Open a lobby` and MUST NOT show a manage-plan button.

#### Scenario: Home shows the game name and the plan

- **WHEN** a signed-in account with game name `Countess` and plan `table` opens `/`
- **THEN** the page shows `Countess`, `Table`, and a `Start a session` button, and it does not show `Open a lobby`

#### Scenario: Home with no plan

- **WHEN** a signed-in account with game name `Countess` and no plan opens `/`
- **THEN** the page shows `Countess` and a `Start a session` button, and it does not show `Corner`, `Table`, `House`, or `Open a lobby`

### Requirement: Start a session opens a new room

Choosing `Start a session` MUST create one room for the signed-in account and MUST open `/?room=<id>` for the id that was created. A failed create MUST leave the person on the home and MUST show `Could not open it. Try again`.

#### Scenario: The button opens the new room

- **WHEN** the person on the home chooses `Start a session` and the created room id is `abc`
- **THEN** the browser opens `/?room=abc`

#### Scenario: A failed create stays on the home

- **WHEN** the person chooses `Start a session` and the room is not created
- **THEN** the home remains and the button shows `Could not open it. Try again`

### Requirement: A room link and a pending checkout skip the home

A signed-in visit with `?room=<id>` MUST enter that room and MUST NOT show the home. A signed-in visit with `?plan=corner`, `?plan=table`, or `?plan=house`, and no current plan, MUST start checkout for that plan and MUST NOT show the home.

#### Scenario: A room link enters the table

- **WHEN** a signed-in person opens `/?room=abc`
- **THEN** that room opens and the home is not shown

#### Scenario: A plan query checks out first

- **WHEN** a signed-in account with no plan opens `/?plan=house`
- **THEN** checkout starts for `house` and the home is not shown

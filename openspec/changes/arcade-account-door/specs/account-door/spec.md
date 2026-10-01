## Purpose

Gives every person at the table an account and a game name, and makes the sign-in and create-account pages look like an arcade night.

## ADDED Requirements

### Requirement: A host registers with a name and a game name

The system MUST create one user for a name, a game name, an email, and a password of at least 8 characters. The name and the game name MUST be trimmed. A name or game name that is empty after trimming, or longer than 40 characters, MUST be refused and MUST NOT create a user. Characters other than those limits MUST be kept, including spaces in the middle. The email comparison MUST ignore case and surrounding spaces. The stored secret MUST be a hash, and no response MUST contain the password. A successful registration MUST sign the host in. A second registration for the same email MUST be refused and MUST NOT create a second user. Two users MAY share a game name.

#### Scenario: First registration stores the names

- **WHEN** a visitor registers name `Ada Lovelace`, game name `Countess`, email `Ada@x.com`, and password `correct-horse`
- **THEN** the response is 200, a session cookie is set, the password is absent from the body, and `GET /account` returns the name `Ada Lovelace`, the game name `Countess`, and the email `ada@x.com`

#### Scenario: Blank game name

- **WHEN** a visitor registers name `Ada`, game name `   `, email `ada@x.com`, and password `correct-horse`
- **THEN** the response is 400 and no user exists for that email

#### Scenario: Name longer than 40 characters

- **WHEN** a visitor registers a name of 41 characters, game name `Countess`, email `ada@x.com`, and password `correct-horse`
- **THEN** the response is 400 and no user exists for that email

#### Scenario: Shared game name

- **WHEN** `ada@x.com` is registered with game name `Countess` and `bea@x.com` registers with game name `Countess`
- **THEN** both responses are 200 and both accounts keep the game name `Countess`

### Requirement: Sign-in stays email and password

A correct email and password MUST set a session cookie and MUST return the same user the registration created, including that user's name and game name. An unknown email and a wrong password MUST both return 401 with the same body.

#### Scenario: Sign in returns the names

- **WHEN** the host who registered `ada@x.com` signs in with `Ada@x.com` and `correct-horse`
- **THEN** the response is 200 and `GET /account` returns the name and game name from registration

#### Scenario: Wrong password looks like an unknown email

- **WHEN** a visitor signs in as `ada@x.com` with `wrong-password`, and another visitor signs in as `nobody@x.com` with `correct-horse`
- **THEN** both responses are 401 and the bodies are equal

### Requirement: Login and register are two pages

The play app MUST show `/login` with an email field, a password field, a sign-in action, and a link to register. It MUST show `/register` with a name field, a game name field, an email field, a password field, a create-account action, and a link to sign in. It MUST NOT show a Google action. Opening `/` with no session MUST go to `/login`. Opening `/` with a session MUST show the existing open-a-lobby action. The title face MUST be Bungee and the form face MUST be Fredoka, on a dark grid background.

#### Scenario: The register page

- **WHEN** a visitor opens `/register` with no session
- **THEN** the page shows name, game name, email, password, and create account, and it does not show Google

#### Scenario: The login page

- **WHEN** a visitor opens `/login` with no session
- **THEN** the page shows email, password, and sign in, and it does not show a name field

### Requirement: A room link requires an account

Opening `?room=<id>` with no session MUST send the browser to `/register?room=<id>` and MUST keep that room id. After a successful register or login that carried the room id, the browser MUST return to `?room=<id>`. The room socket MUST NOT give a seat when the request has no session cookie. A socket with a session MUST sit that account down under the account's game name. One account MUST hold at most one seat. A second socket for the same account MUST NOT take a second seat.

#### Scenario: A room link with no session

- **WHEN** a visitor opens `?room=abc` with no session
- **THEN** the browser is sent to `/register?room=abc`

#### Scenario: Joining after an account

- **WHEN** the account with game name `Countess` connects to a live room with a session cookie
- **THEN** that account receives one seat and the lobby shows the label `Countess` on it

#### Scenario: No cookie takes no seat

- **WHEN** a visitor connects to a live room socket with no session cookie
- **THEN** no seat is issued

#### Scenario: The same account does not sit twice

- **WHEN** an account that already holds a seat connects again
- **THEN** the account still holds one seat

### Requirement: The opener stays host

The account that calls `POST /rooms` MUST be the host of that room. A friend who connects before the opener MUST receive a seat and MUST NOT be the host. Until the opener connects, nobody MUST be able to start the game. Friends MUST NOT be charged.

#### Scenario: A friend arrives first

- **WHEN** the opener has created the room and a different account connects before the opener
- **THEN** the friend has a seat, the friend is not the host, and the game cannot be started

#### Scenario: The opener arrives second

- **WHEN** the opener then connects
- **THEN** the opener's seat is the host and the friend's seat stays a guest

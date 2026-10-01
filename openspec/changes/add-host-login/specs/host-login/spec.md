## Purpose

Gives the host a lasting identity so a later payment can attach to a person, and keeps a shared room link open for guests who have no account.

## ADDED Requirements

### Requirement: A host registers with email and password

The system MUST create one user for an email and a password of at least 8 characters. The email comparison MUST ignore case and surrounding spaces, so `Ada@x.com` and `ada@x.com` are the same user. The stored secret MUST be a hash, and no response MUST contain the password. A successful registration MUST sign the host in and return the user's GUID. A second registration for the same email MUST be refused and MUST NOT create a second user. A password shorter than 8 characters MUST be refused and MUST NOT create a user.

#### Scenario: First registration

- **WHEN** a visitor registers `Ada@x.com` with password `correct-horse`
- **THEN** the response is 200, the body has a GUID `id`, a session cookie is set, and the password is absent from the body

#### Scenario: Same email, different case

- **WHEN** `ada@x.com` is already registered and a visitor registers `Ada@x.com` with a different password
- **THEN** the response is 409 and the original user's GUID is unchanged

#### Scenario: Password too short

- **WHEN** a visitor registers `ada@x.com` with password `short`
- **THEN** the response is 400 and no user exists for that email

### Requirement: A host signs in and out

A correct email and password MUST set a session cookie and MUST return the same GUID the registration created. An unknown email and a wrong password MUST both return 401 with the same body, so the response does not say which one failed. `GET /account` with the cookie MUST return that GUID and the normalized email. Without a cookie it MUST return 401. Signing out MUST clear the cookie. The next `GET /account` MUST return 401.

#### Scenario: Sign in returns the same person

- **WHEN** the host who registered `ada@x.com` signs in with `Ada@x.com` and `correct-horse`
- **THEN** the response is 200, `GET /account` returns the GUID from registration and the email `ada@x.com`

#### Scenario: Wrong password looks like an unknown email

- **WHEN** a visitor signs in as `ada@x.com` with `wrong-password`, and another visitor signs in as `nobody@x.com` with `correct-horse`
- **THEN** both responses are 401 and the bodies are equal

#### Scenario: Sign out

- **WHEN** a signed-in host signs out
- **THEN** the session cookie is cleared and `GET /account` returns 401

### Requirement: Only a signed-in host opens a lobby

`POST /rooms` without a session cookie MUST return 401 and MUST NOT create a room. `POST /rooms` with a session cookie MUST return 200 and a room id, as it does today. The front door, the game page with no `room` query, MUST show an email field, a password field, a create-account action, and a sign-in action when there is no session. It MUST NOT show a Google action. After a session exists, that door MUST show the existing "Open a lobby" action.

#### Scenario: Opening a lobby without a session

- **WHEN** a visitor calls `POST /rooms` with no session cookie
- **THEN** the response is 401 and no room id is issued

#### Scenario: Opening a lobby after sign-in

- **WHEN** a signed-in host calls `POST /rooms`
- **THEN** the response is 200 and the body has a room `id`

#### Scenario: The front door before sign-in

- **WHEN** a visitor opens the game with no `room` query and no session
- **THEN** the page shows email, password, create account, and sign in, and it does not show Google

### Requirement: A guest joins from the link without an account

A room address (`?room=` on the game page, `GET /rooms/{id}`, and the room socket) MUST keep working with no session cookie. A guest who opens a live room link MUST receive a seat.

#### Scenario: Checking a live room with no session

- **WHEN** a visitor calls `GET /rooms/{id}` for a live room with no session cookie
- **THEN** the response is 204

#### Scenario: Joining from the link

- **WHEN** a visitor opens `?room=` for a live room with no session cookie
- **THEN** the visitor receives a seat in that room

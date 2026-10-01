## Why

A host is only a seat for as long as the browser stays connected, so there is no person to attach a card to. Subscriptions need a stable user. This change adds that person before any payment work.

## What Changes

- A host registers and signs in with an email and a password. The API stores one user row per email, with a GUID the host keeps across visits.
- The game's front door (no `room` in the address) shows the login form when there is no session, and the existing "Open a lobby" door after a session exists.
- **BREAKING**: `POST /rooms` requires that session. Without it the API returns 401 and creates no room.
- Joining a room from a link (`?room=`) stays open. A guest does not need an account.
- Google sign-in, Stripe, and play caps stay later. The user row is only email, password hash, and GUID, so a later Google login can find the same email instead of creating a second person.

## Capabilities

### New Capabilities

- `host-login`: email and password registration, a session cookie, a stable user GUID, and a signed-in host as the only caller who may open a lobby.

### Modified Capabilities

## Impact

- `POST /rooms` in `RoomsController` gains an auth check. `GET /rooms/{id}` and the room socket stay anonymous.
- New account routes on the API, a `users` table in Postgres, and a session cookie on `play.friendscorner.app`.
- The Vite front door (`main.ts`, when there is no `room` query) shows login or the lobby door.
- Marketing Play links already open `https://play.friendscorner.app`, which is that front door, so the marketing site does not change in this change.

## Why

The front door is a quiet form, and a friend can sit down from a room link with no name. The night is a game, so the door should look like one, and every person at the table should have an account and a game name.

## What Changes

- `/login` and `/register` on the play app replace the single door. Register collects name, game name, email, and password. Login collects email and password. The pages use a Bungee title, a Fredoka form, and a CSS arcade grid.
- A user row stores the name and the game name. The email stays unique. The game name is a label, so two people can share one. A name or game name that is blank or longer than 40 characters is refused.
- **BREAKING**: a room socket with no session cookie does not give a seat. A room link with no cookie sends the browser to `/register` and returns to that room after an account exists.
- The account that opened the lobby stays the host even if a friend connects first. One account holds one seat. The seat label is the game name.
- Friends still do not pay. The marketing site does not change in this change.

## Capabilities

### New Capabilities

- `account-door`: named accounts, arcade login and register pages, and a session required before a seat.

### Modified Capabilities

## Impact

- `AccountEngine`, `AccountManager`, `AccountUser`, and the `users` table gain name and game name.
- `POST /account` register accepts those fields. `GET /account` returns them. Login stays email and password.
- `POST /rooms` still requires a cookie, and the new room remembers that user as host.
- The room socket refuses a join with no cookie. `Join` takes the user id and game name.
- The Vite app routes `/login` and `/register`, and the lobby labels seats with the game name.

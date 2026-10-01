# Arcade account door

Date: 2026-09-30

Sign-in and create-account are two pages on the play app. Every person who opens a room link has an account before they sit down. The door looks like an arcade night.

## Decisions

- The pages live at `play.friendscorner.app/login` and `play.friendscorner.app/register`. The session cookie `friends.session` is already set on that host. The marketing site does not store a user and does not gain these pages in this slice.
- `/register` collects name, game name, email, and password. `/login` collects email and password.
- The title face is Bungee. The form face is Fredoka. The background is a dark receding grid drawn with CSS. There is no 3D engine.
- The email is the identity. Comparison ignores case and surrounding spaces. Two accounts cannot share an email.
- The game name is a label. Two accounts can share one. It is what a seat shows at the table.
- A password is at least 8 characters and is stored as a hash. A wrong password and an unknown email return the same 401 body.
- A room link still names the room. A browser with no cookie is sent to `/register` and, after an account exists, back to that same `?room=`. A person who already has an account uses `/login` and returns the same way.
- The room socket refuses a join that has no cookie. The page redirect is the door. The socket is the lock.
- The host is the account that opened the lobby, even if a friend connects first. Friends have accounts. They do not pay, and they do not become the host by arriving first.
- A seat's label is the game name. "You" and the host mark stay beside that name. They are not the name.

Out of this slice: a restyle of the marketing site, a unique game name, a second password box, a favorite-game field, and charging friends.

## Pages

Opening `/` with no cookie goes to `/login`. Opening `/` with a cookie shows the lobby that already exists, including "Open a lobby".

Opening `?room=` with no cookie goes to `/register?room=<id>`. That page links to `/login?room=<id>`. After a successful register or login, the browser goes to `?room=<id>`.

Opening `/login` or `/register` with a cookie and no `room` query goes to the lobby. With a `room` query, it goes to that room.

The register form shows, in order: name, game name, email, password, then "Create account", then a link to sign in. The login form shows email, password, "Sign in", and a link to create an account. Errors render under the form. A blank name, a blank game name, and a short password say what was wrong. A duplicate email says that email already has an account. A failed sign-in says the email or password is wrong, without saying which one.

## Account rules

`AccountEngine` decides. It does not hash, store, or read a cookie.

- Name and game name are trimmed. Empty, or only spaces, is refused. Longer than 40 characters is refused. Any other characters are kept, including spaces in the middle and letters outside ASCII.
- Password shorter than 8 characters is refused. That check stays.
- Email is trimmed and lowercased. That check stays.
- Sign-in still returns the user id only when the account exists and the password matches. Otherwise it returns no id.

`AccountManager.Register` takes name, game name, email, and password. It asks the engine, then stores one user. `SignIn` stays email and password.

`AccountUser` and the `users` row gain `Name` and `GameName`. The email stays unique. The password stays a hash. Any row that already exists, and has no name, is filled with the part of the email before `@` for both columns so the migration can require them.

`GET /account` returns the user id, the normalized email, the name, the game name, and the plan. `POST /account` with `register` accepts name, game name, email, and password. `login` stays email and password. A successful call still sets `friends.session`.

## The room lock

`POST /rooms` stays as it is: a cookie is required, and that account is the host of the new room. The room remembers that user id.

`RoomSocketController` reads the cookie before it accepts the socket. No cookie closes the socket and does not take a seat. A cookie loads that account and calls `Join` with the user id and the game name.

`Join` stores the game name on the seat. One account holds one seat. A second socket for that same account does not take another seat. The host seat is the seat of the user id that opened the room. A friend who connects first receives a seat and is not the host. Until the opener connects, the room has no host seat, and nobody can start the game.

The lobby view labels each member with their game name. `isYou` and `isHost` stay separate flags. The old labels "You", "Game Master", and "Player N" are not the visible name.

A live room link with no cookie still does not reveal the game. `GET /rooms/{id}` may say the room exists. It does not give a seat.

## Tests

Write these failing, and stop, before the code that makes them pass.

- A blank or over-long name, and a blank or over-long game name, do not create a user.
- A register with a name and a game name stores both. The same email a second time is still refused. A short password is still refused. The password is absent from the response.
- Sign-in with the wrong password and sign-in with an unknown email return the same body.
- `GET /account` returns the name and the game name for the signed-in user.
- A socket with no cookie does not receive a seat.
- A socket with a cookie sits down under that account's game name.
- The account that opened the room is the host after a friend has already connected.
- `/login` shows email and password. `/register` shows name, game name, email, and password. A room link with no session sends the browser to register and keeps the room id.

## What this does not change

The games, the billing prices, and the marketing page stay as they are. Friends still do not pay. The host's plan still covers the night.

This replaces the earlier rule that a guest joins a live room with no account. The link still names the room. The seat now requires a cookie.

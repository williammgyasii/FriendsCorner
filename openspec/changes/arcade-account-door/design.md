## Context

See proposal.md for why. Today `AccountUser` is an id, an email, and a password hash. `LobbyEngine.Host` is the first seat that called `Join`. The room socket accepts a connection with no cookie. The front door is one form in `main.ts`, not `/login` and `/register`.

## Goals / Non-Goals

**Goals:**

- Name and game name are decided in `AccountEngine` and stored on the user.
- The socket is the lock: no cookie, no seat.
- The host is the user id stored when the room is created.

**Non-Goals:**

- A restyle of the marketing site, a unique game name, a confirm-password field, and charging friends.

## Decisions

### Names are engine rules

`AccountEngine` trims a name and a game name and accepts them only when the trimmed value is 1 to 40 characters. `AccountManager.Register` calls that check, then `IUserAccessor.Add`. Hashing stays in `IPasswordHasher`. The alternative was checking length in the controller. The controller would then own a rule the engine already owns for passwords.

### The room remembers the opener

`IRoomRegistryManager.Create` takes the host user id and stores it on the room. `LobbyEngine.Host` becomes the seated member whose user id is that opener, or null when the opener has not sat down. `CanStart` stays false while `Host` is null. The alternative was leaving host as the first socket. That makes a faster friend the Game Master.

### One account, one seat, one socket

`Join` takes a user id and a game name. If that user id already has a seat, the new socket replaces the old one on that same seat. It does not call `Lobby.Join` again. The alternative was refusing the second socket. A refresh then races the old socket's leave and can fail the new one.

### The cookie stays at the edge

`RoomSocketController` reads `friends.session` before `AcceptWebSocketAsync`. No user id closes the socket. The engine never sees a cookie. `AccountsController` puts the name and game name on the session claims so a reconnect does not need a database read to label the seat. `GET /account` still reads the plan from `SubscriptionManager`.

### The page follows the path

`main.ts` reads `location.pathname`. `/login` and `/register` render the arcade door. `/` with no session redirects to `/login`. `?room=` with no session redirects to `/register?room=`. After a 200 from `/account`, a `room` query returns to `?room=`. Seat labels come from `member.gameName` in `describeLobby`. `isYou` and `isHost` stay flags.

### The grid is CSS

Bungee is loaded for the title and Fredoka for the form. The background is a transformed repeating gradient. No three.js. The alternative was a real 3D scene. It is a new runtime for two forms.

## Risks / Trade-offs

- [Existing user rows have no name] → The migration sets both columns from the email local-part, then makes them required.
- [A replaced socket drops the previous tab] → That tab leaves the seat's socket only. The seat stays. One person, one seat.
- [Claims can be stale if a name changes later] → This slice has no rename. The claims are the name from registration or the last sign-in.

## Migration Plan

Add `Name` and `GameName` on `users` in one EF migration. Existing rows copy the email local-part into both. Roll back by dropping the columns. Rooms are in memory, so a deploy drops live seats. That is the same as a restart today.

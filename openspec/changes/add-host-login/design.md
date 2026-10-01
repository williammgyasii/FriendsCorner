## Context

`POST /rooms` in `RoomsController` creates a room for anyone. The front door in `main.ts` calls that when there is no `room` query. A seat exists only while the socket is open. See proposal.md for why a person has to outlive that socket.

The browser never calls the API host directly. `edge/web` forwards `/rooms`, `/turn`, and `/ws/` to `edge/api`, which forwards room calls into one container. Vite proxies only `/rooms` to Kestrel in development.

## Goals / Non-Goals

**Goals:**

- One user GUID per email, a session cookie, and `POST /rooms` refused without that cookie.
- The front door shows the login form or the existing lobby button.
- Guests still join `?room=` with no cookie.

**Non-Goals:**

- Google sign-in, Stripe, and play caps.
- Requiring a guest to have an account.
- Changing room, game, or socket rules.

## Decisions

### The account policy is an engine, the cookie is the controller

`AccountEngine` owns the rules: trim and lowercase the email, refuse a password shorter than 8 characters, treat an unknown email and a wrong password as one rejection, and return the existing GUID on success. It does not hash, query, or set a cookie.

`AccountManager` runs the sequence. It asks `IUserAccessor` for the row, asks `IPasswordHasher` to hash or check, then asks the engine what that means.

`AccountsController` is the only type that sets or clears the session cookie. It calls the manager and maps the result to 200, 400, 401, or 409. `RoomsController.Create` reads the cookie and returns 401 when it is missing. `Check` and the socket do not read it.

Alternative: ASP.NET Identity's user store, with its roles and tokens tables. Rejected. This change needs one row.

### One `users` table

`UserAccessor` in Infrastructure writes `users`: `id` (GUID, key), `email` (unique, already normalized), `password_hash`. The hash comes from ASP.NET's `PasswordHasher`, behind `IPasswordHasher`, so the engine never references that package.

A later Google login finds this row by email. No Google column in this change.

### The cookie is signed with a configured key

The API container sleeps after 30 minutes, so an in-memory data-protection key would sign everyone out on the next wake. The signing key is configuration, `Auth:TicketKey`, the same way the database URL is configuration: user secrets locally, an env var in production. The cookie is HttpOnly, Secure, and SameSite=Lax.

Alternative: a session row in Postgres. Rejected for this change. A signed cookie is enough until something other than the browser must read the session.

### The edge and the dev proxy forward `/account`

`webRouteFor` and `apiRouteFor` treat `/account` as an API route, and `apiRouteFor` allows GET and POST. Vite's proxy gains `/account` next to `/rooms`. The web worker already forwards the request, so the cookie rides on `play.friendscorner.app`.

### The front door is a function the page renders

`doorLook` returns `login` or `lobby` from whether `GET /account` succeeded. `main.ts` renders that. The test asserts the view, not the canvas. `login` has email, password, create account, and sign in, and no Google control. `lobby` is today's "Open a lobby" button.

## Risks / Trade-offs

- [Container sleep drops an ephemeral signing key] → The key is configuration, covered by a test that two tickets signed with the same key both resolve.
- [`POST /rooms` becomes 401 for old clients] → The only caller is this front door, which signs in first. Guests use the socket, not `POST /rooms`.
- [A shared email is one person] → Registration of a taken email returns 409 and does not change the GUID. Google can attach later. It cannot create a second row for that email.

## Migration Plan

Add the `users` table with an EF migration applied at startup, the same path as the game tables. Deploy the edge route and the API together. A missing `Auth:TicketKey` fails startup, the same as a missing database URL, so a deploy cannot silently mint a key.

Rollback is the previous API: `POST /rooms` is open again, and the `users` table can stay.

## 1. Account names

- [x] 1.1 Write a failing `AccountEngine` test: a blank or 41-character name is refused, and a trimmed name up to 40 characters is kept. Run it and show the failure.
- [x] 1.2 Make that test pass in `AccountEngine`.
- [x] 1.3 Write a failing `AccountManager` test: register stores the name and game name, a blank game name stores nothing, and two users may share a game name. Run it and show the failure.
- [x] 1.4 Make that test pass. `AccountUser` and the `users` row gain the two columns, with a migration that backfills existing rows from the email local-part.
- [x] 1.5 Write a failing API test: register and `GET /account` return the names, a blank name is 400, and a wrong password still matches an unknown email. Run it and show the failure.
- [x] 1.6 Make that test pass in `AccountsController`.

## 2. The seat

- [x] 2.1 Write a failing `LobbyEngine` test: the opener stays host when a friend sits first, and a friend cannot start the game until the opener sits. Run it and show the failure.
- [x] 2.2 Make that test pass. One account holds one seat. The state message includes `gameName`.
- [x] 2.3 Write a failing socket test: no cookie returns 401 and does not take a seat. Run it and show the failure.
- [x] 2.4 Make that test pass. `POST /rooms` remembers the opener. A second socket for the same account keeps the same seat.

## 3. The door

- [x] 3.1 Write a failing `doorLook` test: `/login` is email and password, `/register` is name, game name, email, and password, `/` with no session redirects to `/login`, and `?room=` with no session redirects to `/register?room=`. Run it and show the failure.
- [x] 3.2 Make that test pass, and render both pages with Bungee titles, Fredoka forms, and the CSS grid.
- [x] 3.3 Write a failing lobby label test: a member's label is their game name, and host stays a separate flag. Run it and show the failure.
- [x] 3.4 Make that test pass in `describeLobby`.

## 4. Check

- [x] 4.1 Run the backend and frontend suites. Exercise `/login` and `/register` in the browser.

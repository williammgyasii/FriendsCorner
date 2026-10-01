## 1. Account rules

- [x] 1.1 Write failing `AccountEngine` tests: `Ada@x.com` and `ada@x.com` normalize to the same email, a password shorter than 8 characters is refused, an unknown email and a wrong password are the same rejection, and a match returns the existing GUID. Run them and show the failure.
- [x] 1.2 Add `AccountEngine` so those tests pass. Run the Core account tests.

## 2. Saving the user

- [x] 2.1 Write a failing `AccountManager` test with a fake user accessor and a fake hasher: registering `ada@x.com` with `correct-horse` stores one user, registering that email again returns duplicate and does not add a row, and signing in returns the same GUID. Run it and show the failure.
- [x] 2.2 Add `AccountManager`, `IUserAccessor`, and `IPasswordHasher` so that test passes. Run the Core account tests.

- [x] 2.3 Write a failing `UserAccessor` test against `FriendsCornerDb`: a saved user round-trips by normalized email, and a second insert of that email fails. Run it and show the failure.
- [x] 2.4 Add the `users` table, the EF mapping, and `UserAccessor`, and hash with `PasswordHasher` behind `IPasswordHasher`. Run the accessor test and the existing database tests.

## 3. The session and the lobby door

- [x] 3.1 Write a failing API test: register then `GET /account` returns the GUID and `ada@x.com` with no password in the body; the same email again is 409; `short` is 400; wrong password and unknown email are both 401 with equal bodies; sign-out makes `GET /account` 401. Run it and show the failure.
- [x] 3.2 Add `AccountsController` and the cookie signed with `Auth:TicketKey`. Missing key fails startup. Run the account API tests.

- [x] 3.3 Write a failing `RoomsController` test: `Create` with no user is 401 and the registry gains no room; `Create` with a user still returns a live room id; `Check` on a live room is still 204 with no user. Run it and show the failure.
- [x] 3.4 Make `RoomsController.Create` require the signed-in user, and update the existing create test to pass one. Run the room controller tests.

## 4. Reaching the API

- [x] 4.1 Write failing edge tests: `webRouteFor('/account')` is `api`, and `apiRouteFor` allows GET and POST `/account` and still refuses other methods. Run them and show the failure.
- [x] 4.2 Update `webRouteFor` and `apiRouteFor` so those tests pass. Run the edge tests.

- [x] 4.3 Write a failing `frontend/test/devProxy.test.ts` that loads `vite.config.ts` and expects both `/rooms` and `/account` to proxy to `http://localhost:5250`. Run it and show the failure.
- [x] 4.4 Add `/account` to the Vite proxy. Run `devProxy.test.ts`.

## 5. The front door

- [x] 5.1 Write a failing `doorLook` test: no session is the login view (email, password, create account, sign in, no Google); a session is the lobby view whose action is "Open a lobby". Run it and show the failure.
- [x] 5.2 Add `doorLook` and render it from `main.ts` when there is no `room` query, using `GET /account` with credentials. Run the door test and the frontend suite.

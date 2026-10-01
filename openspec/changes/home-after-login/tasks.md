## 1. The home view

- [x] 1.1 Write a failing `homeLook` test: game name `Countess` and plan `table` show `Countess`, `Table`, `Start a session`, and `Could not open it. Try again`, and do not show `Open a lobby` or `Manage plan`. The same game name with no plan does not show `Corner`, `Table`, or `House`. Run it and show the failure.
- [x] 1.2 Make that test pass in `homeLook`. Run the frontend suite.

## 2. The door

- [x] 2.1 Write a failing `doorLook` test: a signed-in visit with no room is the home, not `Open a lobby`, and `?room=abc` still enters that room. Run it and show the failure.
- [x] 2.2 Make that test pass in `doorLook`. Run the frontend suite.

## 3. The page

- [x] 3.1 Write a failing test for the order `boot` must follow: `?plan=house` with no plan is checkout for `house`, and no plan query is the home from `homeLook`. Run it and show the failure.
- [x] 3.2 Make that test pass. `boot` reads `gameName` from `GET /account`, asks for checkout before the home, renders `homeLook`, and the button keeps the current create-room handler. Run the frontend suite.

## 4. Check

- [x] 4.1 Run the frontend suite. In the browser, a signed-in `/` shows the game name, the plan when there is one, and `Start a session`, and it does not show `Open a lobby`. `/?room=` still opens the table. `/?plan=house` with no plan still starts checkout.

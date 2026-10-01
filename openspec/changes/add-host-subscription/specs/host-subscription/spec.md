## Purpose

Lets a signed-in host subscribe to a monthly plan and records that plan on the user, while a guest on a room link still pays nothing.

## ADDED Requirements

### Requirement: Three monthly plans

The plans MUST be Corner at $15 per month, Table at $20 per month, and House at $50 per month, in US dollars. Corner MUST name these monthly allowances: tic-tac-toe 30, chess 8, letter tiles 8, murder mystery 4. Table MUST name tic-tac-toe 90, chess 24, letter tiles 24, murder mystery 12. House MUST name every game as unlimited. A host on any plan, including no plan, MUST still be able to start tic-tac-toe, chess, letter tiles, and murder mystery. Starting a game MUST NOT be refused because a named allowance is used up.

#### Scenario: Allowances are named and not enforced

- **WHEN** a host whose plan is Corner starts tic-tac-toe for the 31st time
- **THEN** the start is accepted, the same as a start by a host with no plan

### Requirement: Checkout is for a signed-in host

`POST /billing` with `{ "action": "checkout", "plan": "corner" }`, `"table"`, or `"house"` and a session cookie MUST return 200 and a body `{ "url" }` whose value is an absolute https URL. The same call with no session cookie MUST return 401 and MUST NOT create a checkout. An unknown plan, including a missing plan, MUST return 400. A host who already has a plan MUST get 409 from checkout and MUST NOT be sent to a second checkout. `POST /billing` with `{ "action": "portal" }` and a session cookie for a host who has a Stripe customer MUST return 200 and a body `{ "url" }`. A host with no Stripe customer MUST get 404 from the portal action. A guest's room link MUST NOT call billing.

#### Scenario: Checkout for Corner

- **WHEN** a signed-in host with no plan posts checkout for `corner`
- **THEN** the response is 200 and `url` starts with `https://`

#### Scenario: Checkout without a session

- **WHEN** a visitor with no session cookie posts checkout for `table`
- **THEN** the response is 401

#### Scenario: Unknown plan

- **WHEN** a signed-in host posts checkout for `weekly`
- **THEN** the response is 400

#### Scenario: A second checkout is refused

- **WHEN** a host whose plan is already House posts checkout for `corner`
- **THEN** the response is 409

#### Scenario: Portal before any payment

- **WHEN** a signed-in host with no Stripe customer posts the portal action
- **THEN** the response is 404

#### Scenario: Billing is not configured

- **WHEN** a signed-in host posts checkout for `corner` and the Stripe secret is missing
- **THEN** the response is 503 and no checkout exists

### Requirement: The webhook records the plan

`POST /billing/webhook` MUST accept a signed billing event and MUST NOT require a session cookie. A request whose signature does not match MUST return 400 and MUST NOT change the user. A completed checkout for a known user MUST store that user's Stripe customer id and set the plan to the purchased plan. A subscription that ends MUST clear the plan and MUST keep the customer id. Applying the same event twice MUST leave the user unchanged the second time. The browser returning from checkout MUST NOT by itself change the plan. `GET /account` for a signed-in host MUST include `plan` as `corner`, `table`, `house`, or `null`.

#### Scenario: A bad signature changes nothing

- **WHEN** a webhook arrives with a signature that does not match
- **THEN** the response is 400 and the user's plan stays unset

#### Scenario: Checkout completed sets the plan

- **WHEN** a signed webhook says the host's checkout for Table completed
- **THEN** the response is 200 and `GET /account` returns `plan` `table` and the same user GUID as before

#### Scenario: The return page is not the record

- **WHEN** the host's browser opens the checkout return address and no webhook has arrived
- **THEN** `GET /account` still returns `plan` `null`

#### Scenario: Cancel keeps the customer

- **WHEN** a signed webhook says that host's subscription ended
- **THEN** `GET /account` returns `plan` `null` and the portal action returns 200

#### Scenario: The same event twice

- **WHEN** the completed-checkout webhook for Corner is delivered twice
- **THEN** the plan is `corner` after both deliveries and the customer id is unchanged

#### Scenario: The portal changes the plan

- **WHEN** a signed webhook says the host's subscription price is now the Corner price
- **THEN** `GET /account` returns `plan` `corner`

#### Scenario: An older event does not overwrite

- **WHEN** a signed webhook that happened earlier says the plan is Table, after Corner is already recorded
- **THEN** `GET /account` still returns `plan` `corner`

### Requirement: The front door starts checkout only after sign-in

The game page with no `room` query and `?plan=corner`, `?plan=table`, or `?plan=house` MUST show the login form when there is no session, and MUST NOT start checkout. After a session exists, that same address MUST request checkout for that plan. A signed-in host whose plan is recorded MUST see that plan's name and a "Manage plan" action. A signed-in host with no plan MUST see "Open a lobby" and MUST NOT see "Manage plan". "Manage plan" MUST request the portal action.

#### Scenario: A plan link before sign-in

- **WHEN** a visitor opens the game with `?plan=corner` and no session
- **THEN** the page shows the email field, the password field, Create account, and Sign in, and does not request checkout

#### Scenario: A plan link after sign-in

- **WHEN** a signed-in host with no plan opens the game with `?plan=house`
- **THEN** the page requests checkout for `house`

#### Scenario: The door shows the recorded plan

- **WHEN** a signed-in host whose plan is Corner opens the game with no `plan` query
- **THEN** the page shows Corner and Manage plan

### Requirement: The marketing page links to the game

The marketing pricing section MUST show four prices: Free $0, Corner $15, Table $20, and House $50. It MUST show Corner's allowances as 30, 8, 8, and 4, Table's as 90, 24, 24, and 12, and House as unlimited, for tic-tac-toe, chess, letter tiles, and murder mystery in that order. Free MUST link to `https://play.friendscorner.app`. Corner, Table, and House MUST link to `https://play.friendscorner.app/?plan=corner`, `?plan=table`, and `?plan=house`. The marketing page MUST NOT call the billing API and MUST NOT store a user.

#### Scenario: Paid plans leave the marketing site

- **WHEN** a visitor reads the pricing section
- **THEN** Corner links to `https://play.friendscorner.app/?plan=corner` and the page has made no billing request

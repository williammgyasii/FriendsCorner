## Why

A signed-in host is a person, and that person is who pays. The marketing page still offers only a free room, so there is nothing to subscribe to and no record of a plan on the user.

## What Changes

- Three monthly plans, charged to the host only: Corner $15, Table $20, House $50. A guest on a room link is not charged.
- A signed-in host can start Checkout for one plan and open the Customer Portal to change or cancel. The webhook, not the browser's return page, writes the plan onto the user.
- `GET /account` includes the current plan, or none.
- The marketing pricing section keeps Free at $0 and adds the three paid plans. A paid plan link opens the game with that plan. Free still opens the game with no plan.
- Each paid plan names an allowance (Corner: tic-tac-toe 30, chess 8, letter tiles 8, murder mystery 4; Table: 90, 24, 24, 12; House: unlimited). Starting a game does not yet refuse a host who is over that allowance.
- Trial rounds, live Stripe charges, tax, and round caps stay later. No Google sign-in.

## Capabilities

### New Capabilities

- `host-subscription`: Checkout and the Customer Portal for a signed-in host, a webhook that records the plan on the user, and the plan on `GET /account`.

### Modified Capabilities

## Impact

- The `users` table gains a Stripe customer id and a plan. The game API gains `POST /billing` and `POST /billing/webhook`.
- Stripe keys, the webhook secret, and the three price ids are configuration, same as the database URL. They are not committed.
- The edge forwards `/billing` and `/billing/webhook`. Vite proxies `/billing` in development.
- The game front door starts Checkout when the address has `?plan=`, and shows the plan plus a manage action when one is recorded.
- The marketing site (`friendscorner-website`) replaces the single Free pricing block with Free plus Corner, Table, and House. It does not store users or call Stripe.

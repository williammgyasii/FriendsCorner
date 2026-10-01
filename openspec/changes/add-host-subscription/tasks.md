## 1. Plan rules

- [x] 1.1 Write failing `SubscriptionEngine` tests: `corner`, `table`, and `house` are the only plan codes; a newer notice sets the plan and customer id; a notice with no plan clears the plan and keeps the customer id; an older notice leaves Corner in place. Run them and show the failure.
- [x] 1.2 Add `SubscriptionEngine` so those tests pass. Run the Core subscription tests.

## 2. Checkout sequence

- [x] 2.1 Write a failing `SubscriptionManager` test with a fake plan accessor and a fake Stripe accessor: checkout for `corner` returns a URL and does not call Stripe when a plan is already stored; `weekly` is refused; the portal is refused when there is no customer; a missing secret is refused before Stripe is called; applying a Table notice stores the plan, and applying it again does not change the customer id. Run it and show the failure.
- [x] 2.2 Add `SubscriptionManager`, `IHostPlanAccessor`, and `IStripeAccessor` so that test passes. Run the Core subscription tests.

## 3. Saving the plan

- [x] 3.1 Write a failing `HostPlanAccessor` test against `FriendsCornerDb`: a saved customer id and plan round-trip by user id, a second user cannot take the same customer id, and a lookup by customer id returns the user. Run it and show the failure.
- [x] 3.2 Add `stripe_customer_id`, `plan`, and `billing_event_at` to `users`, the EF mapping, and `HostPlanAccessor`. Run the accessor test and the existing database tests.

## 4. Stripe at the edge of the process

- [x] 4.1 Write a failing accessor test: a body signed with the test webhook secret becomes a notice, and a body with the wrong secret is rejected. Run it and show the failure.
- [x] 4.2 Add the Stripe.net accessor behind `IStripeAccessor` so that test passes. Checkout uses mode `subscription`, one price, and no automatic tax. Run the accessor test.

## 5. The billing door

- [x] 5.1 Write a failing API test: checkout for `corner` with a session returns 200 and `{ "url" }` starting with `https://`; no session is 401; `weekly` is 400; a host already on House gets 409; the portal with no customer is 404; a missing secret is 503; a bad webhook signature is 400 and the plan stays unset; a signed Table checkout sets `plan` to `table` on `GET /account`; opening the return address does not; a later older event does not replace Corner; a subscription-ended event clears the plan and the portal then returns 200. Run it and show the failure.
- [x] 5.2 Add `BillingController` and the plan field on `GET /account`. Run the billing API tests and the account API tests.

## 6. Reaching the API

- [x] 6.1 Write failing edge tests: `webRouteFor` sends `/billing` and `/billing/webhook` to the API, and `apiRouteFor` allows POST for both and refuses other methods. Run them and show the failure.
- [x] 6.2 Update `webRouteFor` and `apiRouteFor` so those tests pass. Run the edge tests.

- [x] 6.3 Write a failing `devProxy` test that `/billing` proxies to `http://localhost:5250`. Run it and show the failure.
- [x] 6.4 Add `/billing` to the Vite proxy. Run `devProxy.test.ts`.

## 7. What the host sees

- [x] 7.1 Write a failing `billingLook` test: no session with `?plan=corner` is the login view and does not name a checkout; a session with `?plan=house` and no plan requests checkout for `house`; a stored Corner plan shows Corner and Manage plan; no plan shows Open a lobby and does not show Manage plan. Run it and show the failure.
- [x] 7.2 Add `billingLook` and render it from `main.ts` when there is no `room` query. Run the billing door test and the frontend suite.

- [x] 7.3 Write a failing `pricingLook` test in `friendscorner-website`: Free $0 links to `https://play.friendscorner.app`; Corner $15, Table $20, and House $50 link to `?plan=corner`, `?plan=table`, and `?plan=house`; the allowances are 30/8/8/4, 90/24/24/12, and unlimited. Run it and show the failure.
- [x] 7.4 Render those four cards in the pricing section. Run the pricing test.

## 8. The sandbox

- [x] 8.1 Create the three products and monthly prices in the FriendsCorner Stripe sandbox (test mode): Corner 1500, Table 2000, House 5000, currency `usd`. Verify `stripe prices list` shows those three amounts. Store the price ids, secret key, and webhook secret in user secrets and as wrangler secrets. Do not create live prices.
- [x] 8.2 Point the sandbox webhook at `https://play.friendscorner.app/billing/webhook`. Verify a signed test event is accepted and an unsigned one returns 400.

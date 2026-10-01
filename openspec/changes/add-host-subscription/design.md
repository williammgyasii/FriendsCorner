## Context

`users` is `id`, `email`, and `password_hash`. `GET /account` returns the GUID and the email. `POST /rooms` requires the session cookie. The browser reaches the API only through `edge/web` → `edge/api` → the container, and in development through the Vite proxy. The API has no public hostname. See proposal.md for why the host is the one who pays.

The marketing pricing section is one Free card at $0 in `friendscorner-website`, and that repo does not call this API.

## Goals / Non-Goals

**Goals:**

- Checkout and the Customer Portal run only for a signed-in host, and the webhook is what writes the plan.
- The engine decides what an event means. Stripe's SDK stays in Infrastructure.

**Non-Goals:**

- Enforcing the named allowances, the trial, tax, and live charges.
- A guest account, or a billing call from the marketing site.

## Decisions

### The plan policy is an engine

`SubscriptionEngine` owns the three plan codes `corner`, `table`, and `house`. It accepts a billing notice (event time, customer id, and a plan code or none) and the user's current plan and event time. A newer notice replaces the plan. An older notice is ignored. A notice with no plan clears the plan and keeps the customer id. It does not know price ids, HTTP, or Stripe types.

`SubscriptionManager` runs the sequence. It rejects an unknown plan code, refuses checkout when a plan is already stored, and refuses the portal when no customer id is stored. It asks `IStripeAccessor` for a URL and asks `IHostPlanAccessor` to read and save the row. A missing Stripe secret returns 503 before any Stripe call.

`BillingController` is the HTTP edge. `POST /billing` reads the session cookie and maps the manager's result to 200 `{ "url" }`, 400, 401, 404, 409, or 503. `POST /billing/webhook` does not read the cookie. It asks the accessor to verify the signature, then the manager to apply the notice. A bad signature is 400.

`AccountsController.Current` still owns `GET /account`. It asks `SubscriptionManager` for the plan and adds `plan` to the body. `AccountManager` stays registration and sign-in.

Alternative: one `AccountManager` method that also talks to Stripe. Rejected. Paying and registering change for different reasons.

### The user row gains billing columns, behind a second accessor

`IHostPlanAccessor` reads and writes `stripe_customer_id`, `plan`, and `billing_event_at` on `users`. `AccountUser` stays email and password hash. `IStripeAccessor` creates the checkout session, creates the portal session, and turns a webhook body plus signature into a notice or a rejection. Stripe.net is referenced only by that accessor.

The checkout session is mode `subscription`, one price, no `payment_method_types`, no automatic tax, and Managed Payments off. This sandbox turns Managed Payments on unless the session says otherwise, and that demand rejects a product with no tax code. The customer metadata holds the user GUID. The success and cancel URLs return to `https://play.friendscorner.app`. Price ids are configuration (`Stripe:Prices:Corner`, `Table`, `House`), as are `Stripe:SecretKey` and `Stripe:WebhookSecret`.

The three products and prices are created in the FriendsCorner Stripe sandbox with the CLI. The ids go into user secrets locally and wrangler secrets in production. They are not committed. Live mode is not used.

Alternative: create the customer at registration. Rejected. A host who never pays has no Stripe customer, which is why the portal is 404 until the first checkout.

### The edge forwards billing, including the webhook

`webRouteFor` treats `/billing` and `/billing/webhook` as API routes. `apiRouteFor` allows POST for both and refuses other methods. Stripe calls `https://play.friendscorner.app/billing/webhook`, and the web worker forwards that into the container. Vite proxies `/billing` to `http://localhost:5250`, which also covers `/billing/webhook`.

### The door asks for checkout; it does not decide the plan

`billingLook(signedIn, plan, requestedPlan)` returns `login`, `checkout`, or `lobby`. `login` is today's email form. `checkout` names the requested plan. `lobby` is "Open a lobby", plus the plan name and "Manage plan" only when a plan is stored. `main.ts` renders it from `GET /account` and `?plan=`. A checkout or portal result navigates to `url`.

The marketing page renders four cards from a `pricingLook` list: Free $0 to `https://play.friendscorner.app`, and the three paid plans to `?plan=corner`, `?plan=table`, and `?plan=house`, with the allowances from the spec. That list is the test. The page does not gain a Stripe client.

## Risks / Trade-offs

- [A webhook arrives before the browser returns] → The return address does not write the plan. The door reads `GET /account` after the webhook.
- [Events arrive out of order] → `billing_event_at` keeps the newer notice. The older one is ignored.
- [Allowances are advertised before they are enforced] → The cards name the numbers. Starting a game does not read them. Enforcement is a later change.
- [The container sleeps] → The plan is a column in Postgres, not memory. The webhook secret is configuration, so a wake can still verify a signature.

## Migration Plan

Add the three columns with an EF migration applied at startup. Create the sandbox products, then set the three price ids, the secret key, and the webhook secret as user secrets. Deploy the edge routes and the API together, and set the same values as wrangler secrets before that deploy. Point the sandbox webhook at `https://play.friendscorner.app/billing/webhook`.

Rollback is the previous API. The new columns can stay. Checkout is gone, and `GET /account` returns to id and email.

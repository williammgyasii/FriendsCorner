## Why

Public routes on the play app (`/account`, `/rooms`, `/billing`) sit on the internet with no throttle. A bot can brute-force passwords, spam registrations, or burn Stripe and TURN resources.

## What Changes

- The API Worker rate-limits selected routes before forwarding to the room container.
- Limits use Cloudflare's `ratelimits` binding, keyed by client IP (`CF-Connecting-IP`).
- Over-limit requests return **429** with an empty body.
- `POST /billing/webhook` is excluded (Stripe signature is the lock).

## Capabilities

### New Capabilities

- `api-rate-limit`: edge rate limits on `POST /account`, `POST /rooms`, `POST /billing` (not webhook), and `GET /turn`.

### Modified Capabilities

## Impact

- `edge/api/wrangler.jsonc` gains `ratelimits` bindings.
- `edge/api/src/index.ts` checks limits before proxying.
- Edge tests cover allow, deny, and webhook skip.

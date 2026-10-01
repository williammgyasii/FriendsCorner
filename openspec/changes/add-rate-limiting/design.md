## Context

HTTP enters `play.friendscorner.app` web worker, then the API worker, then the ASP.NET container. Rate limiting belongs at the API worker — the most volatile public HTTP boundary.

## Goals / Non-Goals

**Goals:**
- Throttle abuse on account, room, billing, and TURN routes by IP.
- Return 429 when over limit; otherwise forward unchanged.

**Non-Goals:**
- Per-email login lockout (backend slice later).
- WebSocket connection limits.
- Marketing site rate limits.

## Decisions

**Cloudflare `ratelimits` binding (Option A)** over ASP.NET middleware or dashboard WAF rules — testable in repo, covers `/turn`, runs before the container.

| Binding | Route | Limit | Period |
|---------|-------|-------|--------|
| `ACCOUNT_POST` | `POST /account` | 10 | 60s |
| `ROOM_POST` | `POST /rooms` | 20 | 60s |
| `BILLING_POST` | `POST /billing` | 20 | 60s |
| `TURN_GET` | `GET /turn` | 60 | 60s |

Key: `CF-Connecting-IP`, falling back to the first `X-Forwarded-For` hop.

## Risks / Trade-offs

- Counters are per Cloudflare location, not global → acceptable for v1 abuse protection.
- Shared NAT (school, office) may hit limits together → loosen if reported.
